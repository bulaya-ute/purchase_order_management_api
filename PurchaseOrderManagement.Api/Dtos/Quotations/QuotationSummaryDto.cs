namespace PurchaseOrderManagement.Api.Dtos.Quotations;

/// <summary>Lightweight projection used when listing the standalone quotation library (no line items).</summary>
public class QuotationSummaryDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public int FileId { get; set; }
    public string FileUrl { get; set; } = null!;
    public string? OriginalFileName { get; set; }
    public string? Description { get; set; }
    public string? QuoteReference { get; set; }
    public DateTime QuoteDate { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsExpired { get; set; }
    public string Currency { get; set; } = null!;
    public string? Notes { get; set; }
    public int LineItemCount { get; set; }

    /// <summary>True if at least one of this quotation's line items has been sourced into a SupplierBidItem.</summary>
    public bool IsUsed { get; set; }
}
