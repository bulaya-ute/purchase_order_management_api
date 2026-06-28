namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// One row per supplier competing for a given PO. A PO can have several; zero or one ends up
/// awarded (via PurchaseOrders.AwardedSupplierBidId — there is deliberately no IsAwarded flag here).
/// See docs/02-SUPPLIERS-AND-PROCUREMENT.md.
/// </summary>
public class SupplierBid : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public string? Notes { get; set; }

    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
    public ICollection<SupplierBidItem> SupplierBidItems { get; set; } = new List<SupplierBidItem>();

    /// <summary>Inverse of PurchaseOrders.AwardedSupplierBidId — the PO(s) that awarded this bid (at most one in practice).</summary>
    public ICollection<PurchaseOrder> AwardedByPurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
