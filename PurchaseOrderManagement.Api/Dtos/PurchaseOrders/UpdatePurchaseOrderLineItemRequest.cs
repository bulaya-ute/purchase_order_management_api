using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

public class UpdatePurchaseOrderLineItemRequest
{
    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string Description { get; set; } = null!;

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }

    [Range(0, 100)]
    public decimal? DiscountPercentage { get; set; }

    [Range(0, 100)]
    public decimal? TaxPercentage { get; set; }

    /// <summary>Base64-encoded xmin concurrency token from the last read, if available.</summary>
    public string? RowVersion { get; set; }
}
