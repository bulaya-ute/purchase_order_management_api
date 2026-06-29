using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

public class CreateSupplierBidRequest
{
    [Required]
    public int SupplierId { get; set; }

    [StringLength(2048)]
    public string? Notes { get; set; }
}
