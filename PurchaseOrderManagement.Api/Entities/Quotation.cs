namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// A document a supplier sent for a given bid. A single supplier bid can have multiple
/// quotations. See docs/02-SUPPLIERS-AND-PROCUREMENT.md.
/// </summary>
public class Quotation : BaseEntity
{
    public int SupplierBidId { get; set; }
    public SupplierBid SupplierBid { get; set; } = null!;

    /// <summary>Mandatory — every quotation must have an uploaded file.</summary>
    public int FileId { get; set; }
    public StoredFile File { get; set; } = null!;

    public string? QuoteReference { get; set; }
    public DateTime QuoteDate { get; set; }

    /// <summary>When the supplier's quoted prices lapse. Null = no stated expiry.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    public string? Notes { get; set; }

    public ICollection<QuotationLineItem> QuotationLineItems { get; set; } = new List<QuotationLineItem>();
}
