using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Users;

public class UpdateUserRequest
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

    public bool IsActive { get; set; }

    /// <summary>
    /// Desired full set of role ids. UserRoles are reconciled to match: missing links are added,
    /// removed links are soft-deleted.
    /// </summary>
    public IReadOnlyList<int> RoleIds { get; set; } = Array.Empty<int>();
}
