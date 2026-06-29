using PurchaseOrderManagement.Api.Dtos.Approvals;
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
    public int IssuerUserId { get; set; }
    public string IssuerUserName { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; }
    public string? Notes { get; set; }

    public int? AwardedSupplierBidId { get; set; }
    public DateTime? AwardedAtUtc { get; set; }
    public int? AwardedByUserId { get; set; }

    public DateTime? PaidAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public IReadOnlyList<PurchaseOrderLineItemDto> LineItems { get; set; } = Array.Empty<PurchaseOrderLineItemDto>();
    public IReadOnlyList<ApprovalDto> Approvals { get; set; } = Array.Empty<ApprovalDto>();

    /// <summary>Bid comparison summaries for a bid-based PO; empty for direct-entry POs.</summary>
    public IReadOnlyList<SupplierBidSummaryDto> SupplierBids { get; set; } = Array.Empty<SupplierBidSummaryDto>();

    /// <summary>Base64-encoded xmin concurrency token for the PO header row.</summary>
    public string RowVersion { get; set; } = null!;
}
