using PurchaseOrderManagement.Api.Dtos.Files;

namespace PurchaseOrderManagement.Api.Dtos.Quotations;

public class QuotationDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public FileDto File { get; set; } = null!;
    public string? Description { get; set; }
    public string? QuoteReference { get; set; }
    public DateTime QuoteDate { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsExpired { get; set; }
    public string Currency { get; set; } = null!;
    public string? Notes { get; set; }
    public bool IsUsed { get; set; }

    public decimal? TaxRate { get; set; }
    public decimal? DiscountRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }

    public IReadOnlyList<QuotationLineItemDto> LineItems { get; set; } = Array.Empty<QuotationLineItemDto>();
}
