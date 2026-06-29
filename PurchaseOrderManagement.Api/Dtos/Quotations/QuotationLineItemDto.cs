namespace PurchaseOrderManagement.Api.Dtos.Quotations;

public class QuotationLineItemDto
{
    public int Id { get; set; }
    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
