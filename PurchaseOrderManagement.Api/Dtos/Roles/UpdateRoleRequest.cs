using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Roles;

/// <summary>
/// Role update is a rename only. Re-parenting is intentionally out of scope (it would require
/// re-validating the seniority ceiling and cycle checks across the whole subtree).
/// </summary>
public class UpdateRoleRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = null!;
}
