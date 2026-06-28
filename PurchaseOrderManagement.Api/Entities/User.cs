namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// A system user. Every user belongs to exactly one company row — staff who work across the
/// whole group are assigned to the parent company's row, not left null. See docs/01-IDENTITY-AND-ROLES.md.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = null!;

    /// <summary>Login identifier. Case-insensitive unique via Postgres citext.</summary>
    public string Email { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;

    public bool IsActive { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
