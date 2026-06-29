using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

/// <summary>Selects the winning SupplierBid for a bid-based PO while still in Draft (docs/02/03).</summary>
public class SelectAwardedBidRequest
{
    [Required]
    public int SupplierBidId { get; set; }
}
