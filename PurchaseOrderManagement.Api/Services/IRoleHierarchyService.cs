namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Helpers for reasoning about the self-referencing Roles tree at request time by walking
/// ParentRoleId chains (docs/01-IDENTITY-AND-ROLES.md). No Depth/path column is materialized.
/// </summary>
public interface IRoleHierarchyService
{
    /// <summary>
    /// Resolves the acting user's "ceiling" role: among the roles they currently hold, the one
    /// with the smallest depth from the root (most senior). Returns null if the user holds no roles.
    /// </summary>
    Task<int?> GetCeilingRoleIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// The set of role ids the acting user may set as a parent for a new role: their ceiling role
    /// plus every descendant of it. Empty if the user holds no roles.
    /// </summary>
    Task<IReadOnlyList<int>> GetAllowedParentRoleIdsAsync(int userId, CancellationToken cancellationToken);
}
