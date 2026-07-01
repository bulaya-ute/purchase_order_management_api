namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// A standalone library record of a document a supplier sent — exists independently of any
/// SupplierBid/PurchaseOrder (kept for audit/future reference even if never used). A quotation
/// line item may later be sourced into a SupplierBidItem (any bid for the same supplier), at
/// which point it becomes "used". See docs/02-SUPPLIERS-AND-PROCUREMENT.md.
/// </summary>
public class Quotation : BaseEntity
{
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    /// <summary>Mandatory — every quotation must have an uploaded file.</summary>
    public int FileId { get; set; }
    public StoredFile File { get; set; } = null!;

    public string? Description { get; set; }
    public string? QuoteReference { get; set; }
    public DateTime QuoteDate { get; set; }

    /// <summary>When the supplier's quoted prices lapse. Null = no stated expiry.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    public string CurrencyCode { get; set; } = null!;
    public Currency Currency { get; set; } = null!;

    /// <summary>null = tax pre-included in unit costs; 0 = no tax; >0 = rate applied to Subtotal.</summary>
    public decimal? TaxRate { get; set; }
    /// <summary>null or 0 = no discount; >0 = rate applied post-tax.</summary>
    public decimal? DiscountRate { get; set; }

    public string? Notes { get; set; }

    public ICollection<QuotationLineItem> QuotationLineItems { get; set; } = new List<QuotationLineItem>();
}
