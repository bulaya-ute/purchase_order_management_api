using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// The purchase order itself. Composed during Draft, locked at submit (Draft -> Open).
/// See docs/03-PURCHASE-ORDERS.md.
/// </summary>
public class PurchaseOrder : BaseEntity
{
    /// <summary>Auto-generated, sequential (e.g. "PO-0001"), derived from Id.</summary>
    public string PONumber { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int IssuerUserId { get; set; }
    public User IssuerUser { get; set; } = null!;

    /// <summary>ISO 4217 code. All line items inherit this — no per-line currency.</summary>
    public string Currency { get; set; } = null!;

    public PurchaseOrderStatus Status { get; set; }

    /// <summary>The single winning bid selected by the creator while in Draft, if the PO went through bidding. Locked at submit.</summary>
    public int? AwardedSupplierBidId { get; set; }
    public SupplierBid? AwardedSupplierBid { get; set; }

    public DateTime? AwardedAtUtc { get; set; }
    public int? AwardedByUserId { get; set; }
    public User? AwardedByUser { get; set; }

    /// <summary>Payment milestone — independent of delivery. Null = not yet paid.</summary>
    public DateTime? PaidAtUtc { get; set; }

    /// <summary>Delivery milestone — independent of payment. Null = not yet delivered.</summary>
    public DateTime? DeliveredAtUtc { get; set; }

    /// <summary>Rolled up from line items — not independently entered.</summary>
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public ICollection<SupplierBid> SupplierBids { get; set; } = new List<SupplierBid>();
    public ICollection<PurchaseOrderLineItem> PurchaseOrderLineItems { get; set; } = new List<PurchaseOrderLineItem>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}
