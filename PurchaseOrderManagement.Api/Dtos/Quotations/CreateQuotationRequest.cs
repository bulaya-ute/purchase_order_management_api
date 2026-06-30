using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Quotations;

public class CreateQuotationRequest
{
    [Required]
    public int SupplierId { get; set; }

    /// <summary>FileId is mandatory — every quotation must have an uploaded file (docs/02).</summary>
    [Required]
    public int FileId { get; set; }

    [StringLength(128)]
    public string? QuoteReference { get; set; }

    [Required]
    public DateTime QuoteDate { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>ISO 4217 code. Must reference an active Currency row.</summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = null!;

    [StringLength(2048)]
    public string? Notes { get; set; }

    [MinLength(1, ErrorMessage = "At least one line item is required.")]
    public List<CreateQuotationLineItemRequest> LineItems { get; set; } = new();
}
