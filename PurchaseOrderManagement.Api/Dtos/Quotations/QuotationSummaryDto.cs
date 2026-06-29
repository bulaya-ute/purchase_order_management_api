namespace PurchaseOrderManagement.Api.Dtos.Quotations;

/// <summary>Lightweight projection used when listing quotations for a bid (no line items).</summary>
public class QuotationSummaryDto
{
    public int Id { get; set; }
    public int SupplierBidId { get; set; }
    public int FileId { get; set; }
    public string FileUrl { get; set; } = null!;
    public string? OriginalFileName { get; set; }
    public string? QuoteReference { get; set; }
    public DateTime QuoteDate { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsExpired { get; set; }
    public string? Notes { get; set; }
    public int LineItemCount { get; set; }
}
