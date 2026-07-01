namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

public class AttachSupplierBidRequest
{
    public int SupplierBidId { get; set; }
    public bool IsPrimary { get; set; }
}
