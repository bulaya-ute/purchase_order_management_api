using PurchaseOrderManagement.Api.Dtos.Roles;

namespace PurchaseOrderManagement.Api.Services;

public interface IRoleService
{
    /// <summary>All roles, flat, each with ParentRoleId so the client can build the tree.</summary>
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken);
    Task<RoleDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);
    Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>The roles the current user may set as a parent (their ceiling + its descendants).</summary>
    Task<IReadOnlyList<RoleDto>> GetAllowedParentsAsync(CancellationToken cancellationToken);
}
