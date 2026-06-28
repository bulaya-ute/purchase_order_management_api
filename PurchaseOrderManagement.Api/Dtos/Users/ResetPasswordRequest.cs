using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Users;

public class ResetPasswordRequest
{
    /// <summary>New admin-set password, hashed via IPasswordHasher.</summary>
    [Required]
    [StringLength(256, MinimumLength = 8)]
    public string NewPassword { get; set; } = null!;
}
