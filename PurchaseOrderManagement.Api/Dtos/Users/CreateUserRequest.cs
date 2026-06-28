using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Users;

public class CreateUserRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    public int CompanyId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Admin-set initial password, hashed via IPasswordHasher.</summary>
    [Required]
    [StringLength(256, MinimumLength = 8)]
    public string Password { get; set; } = null!;

    /// <summary>Roles to assign via UserRoles. May be empty.</summary>
    public IReadOnlyList<int> RoleIds { get; set; } = Array.Empty<int>();
}
