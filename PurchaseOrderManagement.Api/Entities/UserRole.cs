namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Many-to-many join between Users and Roles — a user can hold multiple roles simultaneously.
/// See docs/01-IDENTITY-AND-ROLES.md.
/// </summary>
public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
