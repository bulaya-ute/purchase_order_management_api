using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Quotations;

public class CreateQuotationLineItemRequest
{
    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string Description { get; set; } = null!;

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }
}
