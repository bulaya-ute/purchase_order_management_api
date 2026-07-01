using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Roles;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IRoleHierarchyService _hierarchy;

    public RoleService(AppDbContext db, ICurrentUser currentUser, IRoleHierarchyService hierarchy)
    {
        _db = db;
        _currentUser = currentUser;
        _hierarchy = hierarchy;
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Roles.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("No authenticated user.");

        var parentExists = await _db.Roles.AnyAsync(r => r.Id == request.ParentRoleId, cancellationToken);
        if (!parentExists)
        {
            throw ServiceException.Validation($"Parent role {request.ParentRoleId} was not found.");
        }

        // Seniority ceiling rule (docs/01): the new role's parent must be the acting user's most
        // senior held role, or any descendant of it. Walked via ParentRoleId at request time.
        var allowedParentIds = await _hierarchy.GetAllowedParentRoleIdsAsync(userId, cancellationToken);
        if (allowedParentIds.Count == 0)
        {
            throw ServiceException.Forbidden(
                "You hold no roles, so you cannot create roles. Roles can only be created beneath the most senior role you hold.");
        }

        if (!allowedParentIds.Contains(request.ParentRoleId))
        {
            throw ServiceException.Forbidden(
                "The selected parent role is outside your seniority ceiling. A new role's parent must be the most senior role you hold, or one of its descendants.");
        }

        var role = new Role
        {
            Name = request.Name.Trim(),
            ParentRoleId = request.ParentRoleId,
            IsSystemRole = false,
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(role.Id, cancellationToken))!;
    }

    public async Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Role {id} was not found.");

        if (role.IsSystemRole)
        {
            throw ServiceException.Forbidden("System roles cannot be renamed.");
        }

        role.Name = request.Name.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(role.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Role {id} was not found.");

        if (role.IsSystemRole)
            throw ServiceException.Forbidden("System roles cannot be deleted.");

        // Load the full subtree and collect IDs to delete (BFS).
        var allRoles = await _db.Roles.ToListAsync(cancellationToken);
        var toDelete = new List<Role>();
        CollectSubtree(id, allRoles, toDelete);

        // Remove user-role assignments for all roles being deleted.
        var deleteIds = toDelete.Select(r => r.Id).ToList();
        var userRoles = await _db.UserRoles.Where(ur => deleteIds.Contains(ur.RoleId)).ToListAsync(cancellationToken);
        _db.UserRoles.RemoveRange(userRoles);

        // Delete leaves-first to satisfy FK constraints (children before parent).
        // Since we collected depth-first, reversing gives leaves first.
        toDelete.Reverse();
        _db.Roles.RemoveRange(toDelete);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void CollectSubtree(int rootId, List<Role> allRoles, List<Role> result)
    {
        var role = allRoles.FirstOrDefault(r => r.Id == rootId);
        if (role is null) return;
        result.Add(role);
        foreach (var child in allRoles.Where(r => r.ParentRoleId == rootId))
            CollectSubtree(child.Id, allRoles, result);
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllowedParentsAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("No authenticated user.");

        var allowedIds = await _hierarchy.GetAllowedParentRoleIdsAsync(userId, cancellationToken);
        if (allowedIds.Count == 0)
        {
            return Array.Empty<RoleDto>();
        }

        return await _db.Roles.AsNoTracking()
            .Where(r => allowedIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .ThenBy(r => r.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Role, RoleDto>> ToDto =
        r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            ParentRoleId = r.ParentRoleId,
            IsSystemRole = r.IsSystemRole,
        };
}
