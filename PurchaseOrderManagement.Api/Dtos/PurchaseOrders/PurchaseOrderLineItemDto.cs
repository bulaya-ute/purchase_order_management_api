namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

public class PurchaseOrderLineItemDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int? SourceSupplierBidItemId { get; set; }
    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Currency { get; set; } = null!;
    public decimal? DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? TaxPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Base64-encoded xmin concurrency token — echo back on update to detect concurrent edits.</summary>
    public string RowVersion { get; set; } = null!;
}
