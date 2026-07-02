namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// The editable, comparison-ready version of quotation lines, scoped to a bid. Cost/tax/discount
/// rates are tied to the original quote but quantities may be adjusted. SourceQuotationLineItemId
/// must reference a quotation line belonging to the bid's supplier (any quotation, not just ones
/// under "this bid"). Currency defaults from the source quotation's Currency when not explicitly
/// overridden. See docs/02-SUPPLIERS-AND-PROCUREMENT.md.
/// </summary>
public class SupplierBidItem : BaseEntity
{
    public int SupplierBidId { get; set; }
    public SupplierBid SupplierBid { get; set; } = null!;

    /// <summary>Traceability back to the original quoted line. Every bid item must reference a quotation line.</summary>
    public int SourceQuotationLineItemId { get; set; }
    public QuotationLineItem SourceQuotationLineItem { get; set; } = null!;

    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public string CurrencyCode { get; set; } = null!;
    public Currency Currency { get; set; } = null!;

    public decimal? DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal? TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }

    public decimal LineSubtotal { get; set; }
    public decimal LineTotal { get; set; }

    public ICollection<PurchaseOrderLineItem> PurchaseOrderLineItems { get; set; } = new List<PurchaseOrderLineItem>();
}
