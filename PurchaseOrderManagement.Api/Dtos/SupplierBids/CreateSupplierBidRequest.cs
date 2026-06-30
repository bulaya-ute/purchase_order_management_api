using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.SupplierBids;

/// <summary>
/// Creates a standalone bid (PurchaseOrderId is taken from the route when created under a PO,
/// or left null for a library/unattached bid created via the top-level endpoint).
/// </summary>
public class CreateSupplierBidRequest
{
    [Required]
    public int SupplierId { get; set; }

    [StringLength(2048)]
    public string? Notes { get; set; }
}
