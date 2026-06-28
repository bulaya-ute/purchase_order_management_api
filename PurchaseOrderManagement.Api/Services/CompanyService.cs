using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.Companies;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;

    public CompanyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CompanyDto>> ListAsync(PagedQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = _db.Companies.AsNoTracking().OrderBy(c => c.Name).ThenBy(c => c.Id);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentCompanyId = c.ParentCompanyId,
                ParentCompanyName = c.ParentCompany != null ? c.ParentCompany.Name : null,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CompanyDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<CompanyDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Companies.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentCompanyId = c.ParentCompanyId,
                ParentCompanyName = c.ParentCompany != null ? c.ParentCompany.Name : null,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (request.ParentCompanyId is int parentId)
        {
            await EnsureParentExistsAsync(parentId, cancellationToken);
        }

        var company = new Company
        {
            Name = name,
            ParentCompanyId = request.ParentCompanyId,
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(company.Id, cancellationToken))!;
    }

    public async Task<CompanyDto> UpdateAsync(int id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Company {id} was not found.");

        if (request.ParentCompanyId is int parentId)
        {
            if (parentId == id)
            {
                throw ServiceException.Validation("A company cannot be its own parent.");
            }

            await EnsureParentExistsAsync(parentId, cancellationToken);
            await EnsureNoCycleAsync(id, parentId, cancellationToken);
        }

        company.Name = request.Name.Trim();
        company.ParentCompanyId = request.ParentCompanyId;

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(company.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Company {id} was not found.");

        // Block deletes that would orphan dependents (the global filter already hides soft-deleted rows).
        var hasChildren = await _db.Companies.AnyAsync(c => c.ParentCompanyId == id, cancellationToken);
        if (hasChildren)
        {
            throw ServiceException.Conflict("Cannot delete a company that still has child companies.");
        }

        var hasUsers = await _db.Users.AnyAsync(u => u.CompanyId == id, cancellationToken);
        if (hasUsers)
        {
            throw ServiceException.Conflict("Cannot delete a company that still has users.");
        }

        // Remove() is converted to a soft delete by AppDbContext (docs/05).
        _db.Companies.Remove(company);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureParentExistsAsync(int parentId, CancellationToken cancellationToken)
    {
        var exists = await _db.Companies.AnyAsync(c => c.Id == parentId, cancellationToken);
        if (!exists)
        {
            throw ServiceException.Validation($"Parent company {parentId} was not found.");
        }
    }

    /// <summary>
    /// Walks the proposed parent's ancestor chain; if it reaches the company being updated, the new
    /// link would make the company its own ancestor (a cycle). Walked at request time, no Depth column.
    /// </summary>
    private async Task EnsureNoCycleAsync(int companyId, int proposedParentId, CancellationToken cancellationToken)
    {
        var parentMap = await _db.Companies
            .Select(c => new { c.Id, c.ParentCompanyId })
            .ToDictionaryAsync(c => c.Id, c => c.ParentCompanyId, cancellationToken);

        int? current = proposedParentId;
        var guard = parentMap.Count + 1;

        while (current.HasValue)
        {
            if (current.Value == companyId)
            {
                throw ServiceException.Validation("The selected parent would create a cycle (a company cannot be its own ancestor).");
            }

            if (!parentMap.TryGetValue(current.Value, out var next))
            {
                break;
            }

            current = next;

            if (--guard < 0)
            {
                break;
            }
        }
    }
}
