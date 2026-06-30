using PurchaseOrderManagement.Api.Dtos.Common;

namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

/// <summary>
/// Drives the bid-comparison cards when viewing a PO, and rows in the standalone bids library
/// (docs/07): supplier name, per-currency totals, item count, attach state, and quotation expiry status.
/// </summary>
public class SupplierBidSummaryDto
{
    public int Id { get; set; }

    /// <summary>Null = standalone/unattached bid.</summary>
    public int? PurchaseOrderId { get; set; }

    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public string? Notes { get; set; }

    /// <summary>Per-currency totals across the bid's items. Never converted/combined.</summary>
    public IReadOnlyList<CurrencyTotalDto> Totals { get; set; } = Array.Empty<CurrencyTotalDto>();

    public int ItemCount { get; set; }
    public int QuotationCount { get; set; }

    /// <summary>True if the bid has at least one quotation whose ExpiresAtUtc is in the past.</summary>
    public bool HasExpiredQuotation { get; set; }

    /// <summary>Earliest upcoming quotation expiry across the bid's quotations, if any.</summary>
    public DateTime? EarliestQuotationExpiryUtc { get; set; }
}
