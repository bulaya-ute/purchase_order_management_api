using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
