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

    /// <summary>Who the purchase is *for* (e.g. a branch), distinct from CompanyId (the issuing entity). Editable only in Draft.</summary>
    public int? TargetCompanyId { get; set; }
    public Company? TargetCompany { get; set; }

    public int IssuerUserId { get; set; }
    public User IssuerUser { get; set; } = null!;

    /// <summary>
    /// ISO 4217 code. For direct-entry POs (no awarded bid with items) this is the single
    /// currency for the whole PO and the flat Subtotal/TaxAmount/TotalAmount are authoritative.
    /// For bid-based POs this remains the PO's nominal/header currency, but the authoritative
    /// totals live in CurrencyTotals (a vector, since bid items may span multiple currencies).
    /// </summary>
    public string CurrencyCode { get; set; } = null!;
    public Currency Currency { get; set; } = null!;

    public PurchaseOrderStatus Status { get; set; }

    /// <summary>Null = free-form/"Custom" approval chain composed manually in Draft. When set, the approval chain is auto-generated from the type's steps and immutable.</summary>
    public int? PurchaseOrderTypeId { get; set; }
    public PurchaseOrderType? PurchaseOrderType { get; set; }

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

    /// <summary>Rolled up from line items — not independently entered. Authoritative for direct-entry POs; for bid-based POs with multi-currency line items, see CurrencyTotals.</summary>
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public ICollection<SupplierBid> SupplierBids { get; set; } = new List<SupplierBid>();
    public ICollection<PurchaseOrderLineItem> PurchaseOrderLineItems { get; set; } = new List<PurchaseOrderLineItem>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<PurchaseOrderCurrencyTotal> CurrencyTotals { get; set; } = new List<PurchaseOrderCurrencyTotal>();
}
