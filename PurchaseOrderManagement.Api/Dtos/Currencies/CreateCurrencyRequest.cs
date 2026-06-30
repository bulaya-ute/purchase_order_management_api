using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Currencies;

public class CreateCurrencyRequest
{
    /// <summary>ISO 4217 code, e.g. "ZMW". Stored upper-invariant.</summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
