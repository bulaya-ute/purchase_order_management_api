namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

public class PurchaseOrderSupplierBidDto
{
    public int SupplierBidId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime AddedAtUtc { get; set; }
}
