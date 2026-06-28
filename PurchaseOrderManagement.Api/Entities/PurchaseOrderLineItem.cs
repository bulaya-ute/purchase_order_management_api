namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// The final, locked-in lines that make up the PO. For a bid-based PO these are created at the
/// moment the PO is Approved (copied from the awarded SupplierBidItems); for a direct-entry PO
/// they are typed in Draft and locked at submit. See docs/03-PURCHASE-ORDERS.md.
/// </summary>
public class PurchaseOrderLineItem : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>Traceability back to the awarded bid item, if this PO went through quotations. Nullable for direct-entry POs.</summary>
    public int? SourceSupplierBidItemId { get; set; }
    public SupplierBidItem? SourceSupplierBidItem { get; set; }

    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public decimal? DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal? TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }

    public decimal LineSubtotal { get; set; }
    public decimal LineTotal { get; set; }
}
