using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

/// <summary>
/// Computed money fields are recomputed server-side on update; only the editable inputs are accepted.
/// </summary>
public class UpdateSupplierBidItemRequest
{
    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string Description { get; set; } = null!;

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = null!;

    [Range(0, 100)]
    public decimal? DiscountPercentage { get; set; }

    [Range(0, 100)]
    public decimal? TaxPercentage { get; set; }

    /// <summary>Optional: base64-encoded xmin token from the last read, to honor optimistic concurrency.</summary>
    public string? RowVersion { get; set; }
}
