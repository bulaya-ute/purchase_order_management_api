using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.PurchaseOrderTypes;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Admin CRUD for PO type presets (plan section C): a fixed, immutable approval chain plus a
/// restriction on which roles may create a PO of the type. Same IAdminAuthorizer.RequireAdmin()
/// pattern as every other admin entity (Services/AdminAuthorizer.cs).
/// </summary>
public class PurchaseOrderTypeService : IPurchaseOrderTypeService
{
    private readonly AppDbContext _db;
    private readonly IAdminAuthorizer _adminAuthorizer;

    public PurchaseOrderTypeService(AppDbContext db, IAdminAuthorizer adminAuthorizer)
    {
        _db = db;
        _adminAuthorizer = adminAuthorizer;
    }

    public async Task<IReadOnlyList<PurchaseOrderTypeDto>> ListAsync(CancellationToken cancellationToken)
    {
        var types = await LoadWithDetailsAsync(cancellationToken).ToListAsync(cancellationToken);
        return types.OrderBy(t => t.Name).ThenBy(t => t.Id).Select(ToDto).ToList();
    }

    public async Task<PurchaseOrderTypeDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var type = await LoadWithDetailsAsync(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return type is null ? null : ToDto(type);
    }

    public async Task<PurchaseOrderTypeDto> CreateAsync(CreatePurchaseOrderTypeRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();

        await ValidateStepsAsync(request.ApprovalSteps, cancellationToken);
        await ValidateRolesAsync(request.AllowedCreatorRoleIds, cancellationToken);

        var type = new PurchaseOrderType
        {
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
        };

        foreach (var step in request.ApprovalSteps)
        {
            type.ApprovalSteps.Add(new PurchaseOrderTypeApprovalStep
            {
                RequiredRoleId = step.RequiredRoleId,
                RequiredUserId = step.RequiredUserId,
                SequenceOrder = step.SequenceOrder,
            });
        }

        foreach (var roleId in request.AllowedCreatorRoleIds.Distinct())
        {
            type.AllowedCreatorRoles.Add(new PurchaseOrderTypeAllowedCreatorRole { RoleId = roleId });
        }

        _db.PurchaseOrderTypes.Add(type);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(type.Id, cancellationToken))!;
    }

    public async Task<PurchaseOrderTypeDto> UpdateAsync(int id, UpdatePurchaseOrderTypeRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();

        var type = await _db.PurchaseOrderTypes
            .Include(t => t.ApprovalSteps)
            .Include(t => t.AllowedCreatorRoles)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Purchase order type {id} was not found.");

        await ValidateStepsAsync(request.ApprovalSteps, cancellationToken);
        await ValidateRolesAsync(request.AllowedCreatorRoleIds, cancellationToken);

        type.Name = request.Name.Trim();
        type.IsActive = request.IsActive;

        // Full-replace semantics for the template's steps/allowed-roles — existing POs already
        // created from a previous version of this type keep their already-generated Approval
        // rows untouched (immutability is per-PO, not retroactive to the template).
        _db.PurchaseOrderTypeApprovalSteps.RemoveRange(type.ApprovalSteps);
        _db.PurchaseOrderTypeAllowedCreatorRoles.RemoveRange(type.AllowedCreatorRoles);

        type.ApprovalSteps.Clear();
        type.AllowedCreatorRoles.Clear();

        foreach (var step in request.ApprovalSteps)
        {
            type.ApprovalSteps.Add(new PurchaseOrderTypeApprovalStep
            {
                RequiredRoleId = step.RequiredRoleId,
                RequiredUserId = step.RequiredUserId,
                SequenceOrder = step.SequenceOrder,
            });
        }

        foreach (var roleId in request.AllowedCreatorRoleIds.Distinct())
        {
            type.AllowedCreatorRoles.Add(new PurchaseOrderTypeAllowedCreatorRole { RoleId = roleId });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(type.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        RequireAdmin();

        var type = await _db.PurchaseOrderTypes
            .Include(t => t.ApprovalSteps)
            .Include(t => t.AllowedCreatorRoles)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Purchase order type {id} was not found.");

        var inUse = await _db.PurchaseOrders.AnyAsync(po => po.PurchaseOrderTypeId == id, cancellationToken);
        if (inUse)
        {
            throw ServiceException.Conflict("Cannot delete a purchase order type that has already been used by a purchase order. Deactivate it instead.");
        }

        _db.PurchaseOrderTypeApprovalSteps.RemoveRange(type.ApprovalSteps);
        _db.PurchaseOrderTypeAllowedCreatorRoles.RemoveRange(type.AllowedCreatorRoles);
        _db.PurchaseOrderTypes.Remove(type);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateStepsAsync(List<CreatePurchaseOrderTypeApprovalStepRequest> steps, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            var hasRole = step.RequiredRoleId.HasValue;
            var hasUser = step.RequiredUserId.HasValue;
            if (hasRole == hasUser)
            {
                throw ServiceException.Validation("Exactly one of RequiredRoleId or RequiredUserId must be supplied for each approval step.");
            }

            if (step.RequiredRoleId is int roleId)
            {
                var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
                if (!roleExists)
                {
                    throw ServiceException.Validation($"Role {roleId} was not found.");
                }
            }

            if (step.RequiredUserId is int userId)
            {
                var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
                if (!userExists)
                {
                    throw ServiceException.Validation($"User {userId} was not found.");
                }
            }
        }
    }

    private async Task ValidateRolesAsync(List<int> roleIds, CancellationToken cancellationToken)
    {
        foreach (var roleId in roleIds.Distinct())
        {
            var exists = await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
            if (!exists)
            {
                throw ServiceException.Validation($"Role {roleId} was not found.");
            }
        }
    }

    private void RequireAdmin()
    {
        if (!_adminAuthorizer.IsAdmin())
        {
            throw ServiceException.Forbidden("Purchase order type management requires an admin-tier role.");
        }
    }

    private IQueryable<PurchaseOrderType> LoadWithDetailsAsync(CancellationToken cancellationToken) =>
        _db.PurchaseOrderTypes.AsNoTracking()
            .Include(t => t.ApprovalSteps).ThenInclude(s => s.RequiredRole)
            .Include(t => t.ApprovalSteps).ThenInclude(s => s.RequiredUser)
            .Include(t => t.AllowedCreatorRoles).ThenInclude(r => r.Role);

    private static PurchaseOrderTypeDto ToDto(PurchaseOrderType type) => new()
    {
        Id = type.Id,
        Name = type.Name,
        IsActive = type.IsActive,
        ApprovalSteps = type.ApprovalSteps
            .OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id)
            .Select(s => new Dtos.PurchaseOrderTypes.PurchaseOrderTypeApprovalStepDto
            {
                Id = s.Id,
                RequiredRoleId = s.RequiredRoleId,
                RequiredRoleName = s.RequiredRole?.Name,
                RequiredUserId = s.RequiredUserId,
                RequiredUserName = s.RequiredUser?.FullName,
                SequenceOrder = s.SequenceOrder,
            })
            .ToList(),
        AllowedCreatorRoleIds = type.AllowedCreatorRoles.Select(r => r.RoleId).ToList(),
        AllowedCreatorRoleNames = type.AllowedCreatorRoles.Select(r => r.Role.Name).OrderBy(n => n).ToList(),
    };
}
