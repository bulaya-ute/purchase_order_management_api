namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

/// <summary>Full bid view including its comparison-ready line items.</summary>
public class SupplierBidDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public string? Notes { get; set; }
    public decimal BidTotal { get; set; }
    public int ItemCount { get; set; }
    public IReadOnlyList<SupplierBidItemDto> Items { get; set; } = Array.Empty<SupplierBidItemDto>();

    /// <summary>Base64-encoded xmin concurrency token for the bid row.</summary>
    public string RowVersion { get; set; } = null!;
}
