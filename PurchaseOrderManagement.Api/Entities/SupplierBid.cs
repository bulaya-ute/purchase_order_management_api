namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// A standalone library record of one supplier's competing offer — can exist on its own
/// (PurchaseOrderId null) and later be attached to a PO (by setting PurchaseOrderId) while that
/// PO is still in Draft. At most one bid per PO ends up awarded (via
/// PurchaseOrders.AwardedSupplierBidId — there is deliberately no IsAwarded flag here).
/// See docs/02-SUPPLIERS-AND-PROCUREMENT.md.
/// </summary>
public class SupplierBid : BaseEntity
{
    /// <summary>Null = standalone/unattached bid. Set when attached to a Draft PO.</summary>
    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public string? Notes { get; set; }

    public ICollection<SupplierBidItem> SupplierBidItems { get; set; } = new List<SupplierBidItem>();

    /// <summary>Inverse of PurchaseOrders.AwardedSupplierBidId — the PO(s) that awarded this bid (at most one in practice).</summary>
    public ICollection<PurchaseOrder> AwardedByPurchaseOrders { get; set; } = new List<PurchaseOrder>();

    public ICollection<PurchaseOrderSupplierBid> PurchaseOrderAttachments { get; set; } = new List<PurchaseOrderSupplierBid>();
}
