using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

/// <summary>Attaches an existing standalone bid to a Draft purchase order.</summary>
public class AttachSupplierBidRequest
{
    [Required]
    public int PurchaseOrderId { get; set; }
}
