using PurchaseOrderManagement.Api.Dtos.Approvals;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.SupplierBids;
using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

/// <summary>
/// Full PO view: header, status, totals, awarded bid, milestones, line items, approvals, and
/// (for bid-based POs) the bid comparison summaries (docs/03).
/// </summary>
public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string PONumber { get; set; } = null!;
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = null!;

    /// <summary>Who the purchase is for (a branch/company), distinct from CompanyId. Null = not set.</summary>
    public int? TargetCompanyId { get; set; }
    public string? TargetCompanyName { get; set; }

    public int IssuerUserId { get; set; }
    public string IssuerUserName { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; }
    public string? Notes { get; set; }

    public int? PurchaseOrderTypeId { get; set; }
    public string? PurchaseOrderTypeName { get; set; }

    public int? AwardedSupplierBidId { get; set; }
    public DateTime? AwardedAtUtc { get; set; }
    public int? AwardedByUserId { get; set; }

    public DateTime? PaidAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }

    /// <summary>
    /// Authoritative single-currency totals for a direct-entry PO (or a bid-based PO whose
    /// awarded bid's items are all in the PO's own Currency). For a bid-based PO with line items
    /// spanning more than one currency, these flat fields are 0 and <see cref="Totals"/> is the
    /// authoritative vector instead — check <see cref="HasMultiCurrencyTotals"/>.
    /// </summary>
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>True when this PO's totals must be read from <see cref="Totals"/> rather than the flat Subtotal/TaxAmount/TotalAmount fields (a bid-based PO whose awarded bid has line items in more than one currency).</summary>
    public bool HasMultiCurrencyTotals { get; set; }

    /// <summary>Per-currency totals, populated only when HasMultiCurrencyTotals is true.</summary>
    public IReadOnlyList<CurrencyTotalDto> Totals { get; set; } = Array.Empty<CurrencyTotalDto>();

    public DateTime CreatedAtUtc { get; set; }

    public IReadOnlyList<PurchaseOrderLineItemDto> LineItems { get; set; } = Array.Empty<PurchaseOrderLineItemDto>();
    public IReadOnlyList<ApprovalDto> Approvals { get; set; } = Array.Empty<ApprovalDto>();

    /// <summary>Bid comparison summaries for a bid-based PO; empty for direct-entry POs.</summary>
    public IReadOnlyList<SupplierBidSummaryDto> SupplierBids { get; set; } = Array.Empty<SupplierBidSummaryDto>();

    /// <summary>Junction rows linking all attached Supplier Bids to this PO (primary and alternatives).</summary>
    public IReadOnlyList<PurchaseOrderSupplierBidDto> AttachedSupplierBids { get; set; } = [];

    /// <summary>Base64-encoded xmin concurrency token for the PO header row.</summary>
    public string RowVersion { get; set; } = null!;
}
