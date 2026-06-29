using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Quotations;

public class CreateQuotationRequest
{
    /// <summary>FileId is mandatory — every quotation must have an uploaded file (docs/02).</summary>
    [Required]
    public int FileId { get; set; }

    [StringLength(128)]
    public string? QuoteReference { get; set; }

    [Required]
    public DateTime QuoteDate { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    [StringLength(2048)]
    public string? Notes { get; set; }

    [MinLength(1, ErrorMessage = "At least one line item is required.")]
    public List<CreateQuotationLineItemRequest> LineItems { get; set; } = new();
}
