using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Suppliers;

public class UpdateSupplierRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string SupplierName { get; set; } = null!;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string Phone { get; set; } = null!;

    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string Address { get; set; } = null!;
}
