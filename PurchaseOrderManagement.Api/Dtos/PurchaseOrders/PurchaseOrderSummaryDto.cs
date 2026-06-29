using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

/// <summary>Row shape for the paged purchase-order list.</summary>
public class PurchaseOrderSummaryDto
{
    public int Id { get; set; }
    public string PONumber { get; set; } = null!;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = null!;
    public int IssuerUserId { get; set; }
    public string IssuerUserName { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
