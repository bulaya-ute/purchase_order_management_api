namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// The items exactly as quoted by the supplier in a specific quotation document. Treated as
/// an immutable record of what was received. See docs/02-SUPPLIERS-AND-PROCUREMENT.md.
/// </summary>
public class QuotationLineItem : BaseEntity
{
    public int QuotationId { get; set; }
    public Quotation Quotation { get; set; } = null!;

    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public ICollection<SupplierBidItem> SupplierBidItems { get; set; } = new List<SupplierBidItem>();
}
