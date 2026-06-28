namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Pragmatic, role-name-based authorization gate for Admin-slice mutations.
///
/// ASSUMPTION (pending the open Q7 in docs/06 — fine-grained permissions are not yet designed):
/// Company and User mutations require the acting user to hold a system/admin-tier role, identified
/// here purely by role name ("Super Admin" or "Admin"). This is deliberately coarse and based only
/// on <see cref="ICurrentUser.Roles"/> (the role names already in the auth cookie). When Q7 lands and
/// a real permission model exists, replace this with a permission check rather than a name match.
///
/// Role *creation* is NOT gated by this — it is governed by the seniority-ceiling rule
/// (see <see cref="IRoleHierarchyService"/> and docs/01-IDENTITY-AND-ROLES.md).
/// </summary>
public interface IAdminAuthorizer
{
    /// <summary>True if the acting user holds an admin-tier role name.</summary>
    bool IsAdmin();
}
