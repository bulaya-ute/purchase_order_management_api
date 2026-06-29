namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

/// <summary>
/// Drives the bid-comparison cards when viewing a PO (docs/07): supplier name, bid total
/// (sum of SupplierBidItems.LineTotal), item count, and quotation expiry status.
/// </summary>
public class SupplierBidSummaryDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public string? Notes { get; set; }
    public decimal BidTotal { get; set; }
    public int ItemCount { get; set; }
    public int QuotationCount { get; set; }

    /// <summary>True if the bid has at least one quotation whose ExpiresAtUtc is in the past.</summary>
    public bool HasExpiredQuotation { get; set; }

    /// <summary>Earliest upcoming quotation expiry across the bid's quotations, if any.</summary>
    public DateTime? EarliestQuotationExpiryUtc { get; set; }
}
