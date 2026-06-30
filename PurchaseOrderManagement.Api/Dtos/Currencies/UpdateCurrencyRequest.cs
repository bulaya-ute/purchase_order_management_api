using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Currencies;

public class UpdateCurrencyRequest
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }
}
