using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.Suppliers;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;

    public SupplierService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SupplierDto>> ListAsync(SupplierListQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = _db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            baseQuery = baseQuery.Where(s => EF.Functions.ILike(s.SupplierName, $"%{search}%"));
        }

        var ordered = baseQuery.OrderBy(s => s.SupplierName).ThenBy(s => s.Id);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ProjectToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<SupplierDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Suppliers.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            SupplierName = request.SupplierName.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim(),
            Address = request.Address.Trim(),
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(supplier.Id, cancellationToken))!;
    }

    public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Supplier {id} was not found.");

        supplier.SupplierName = request.SupplierName.Trim();
        supplier.Phone = request.Phone.Trim();
        supplier.Email = request.Email.Trim();
        supplier.Address = request.Address.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(supplier.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Supplier {id} was not found.");

        var hasBids = await _db.SupplierBids.AnyAsync(sb => sb.SupplierId == id, cancellationToken);
        if (hasBids)
        {
            throw ServiceException.Conflict("Cannot delete a supplier that has supplier bids on record.");
        }

        // Remove() is converted to a soft delete by AppDbContext (docs/05).
        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Supplier, SupplierDto>> ProjectToDto =
        s => new SupplierDto
        {
            Id = s.Id,
            SupplierName = s.SupplierName,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
        };
}
