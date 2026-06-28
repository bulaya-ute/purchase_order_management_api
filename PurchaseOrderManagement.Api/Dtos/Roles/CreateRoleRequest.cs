using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Roles;

public class CreateRoleRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Parent of the new role. Must be within the acting user's seniority ceiling: their most
    /// senior held role, or any descendant of it (docs/01-IDENTITY-AND-ROLES.md). Required —
    /// only the seeded root (Super Admin) has a null parent and it is not created via the API.
    /// </summary>
    [Required]
    public int ParentRoleId { get; set; }
}
