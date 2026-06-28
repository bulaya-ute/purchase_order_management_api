namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// A self-referencing tree of roles (not a flat list). Custom roles are just new rows.
/// Shared globally across the whole company group — does not carry a CompanyId.
/// See docs/01-IDENTITY-AND-ROLES.md.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = null!;

    /// <summary>Null only for the root role (Super Admin). Every other role has a parent.</summary>
    public int? ParentRoleId { get; set; }
    public Role? ParentRole { get; set; }

    /// <summary>True for seeded/protected roles (e.g. Super Admin, Admin) that aren't deletable via the UI.</summary>
    public bool IsSystemRole { get; set; }

    public ICollection<Role> ChildRoles { get; set; } = new List<Role>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Approval> RequiredForApprovals { get; set; } = new List<Approval>();
}
