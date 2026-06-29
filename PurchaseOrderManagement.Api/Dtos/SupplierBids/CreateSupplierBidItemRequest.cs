using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

/// <summary>
/// Computed money fields (LineSubtotal/DiscountAmount/TaxAmount/LineTotal) are intentionally absent:
/// the server computes and persists them, never trusting client-supplied amounts (docs/02).
/// </summary>
public class CreateSupplierBidItemRequest
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

    /// <summary>Optional traceability back to the originating quotation line.</summary>
    public int? SourceQuotationLineItemId { get; set; }
}
