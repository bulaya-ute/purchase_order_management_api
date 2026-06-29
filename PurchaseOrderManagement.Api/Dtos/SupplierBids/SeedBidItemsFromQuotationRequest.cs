using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

public class SeedBidItemsFromQuotationRequest
{
    /// <summary>The quotation whose line items are copied into the bid as editable bid items.</summary>
    [Required]
    public int QuotationId { get; set; }
}
