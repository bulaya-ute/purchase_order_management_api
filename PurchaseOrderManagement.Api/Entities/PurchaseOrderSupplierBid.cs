namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Links all supplier bids attached to a PO — both the primary selection and any alternatives
/// added for approver comparison. Once IsPrimary = true exists for a PO, the set is locked.
/// See docs/02-SUPPLIERS-AND-PROCUREMENT.md and docs/03-PURCHASE-ORDERS.md.
/// </summary>
public class PurchaseOrderSupplierBid
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public int SupplierBidId { get; set; }
    public SupplierBid SupplierBid { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public DateTime AddedAtUtc { get; set; }
}
