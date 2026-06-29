using PurchaseOrderManagement.Api.Dtos.Files;

namespace PurchaseOrderManagement.Api.Dtos.Quotations;

public class QuotationDto
{
    public int Id { get; set; }
    public int SupplierBidId { get; set; }
    public FileDto File { get; set; } = null!;
    public string? QuoteReference { get; set; }
    public DateTime QuoteDate { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsExpired { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<QuotationLineItemDto> LineItems { get; set; } = Array.Empty<QuotationLineItemDto>();
}
