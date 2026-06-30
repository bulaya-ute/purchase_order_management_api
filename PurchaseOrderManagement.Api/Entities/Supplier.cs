namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Global, shared across all companies in the group. See docs/02-SUPPLIERS-AND-PROCUREMENT.md.
/// </summary>
public class Supplier : BaseEntity
{
    public string SupplierName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Address { get; set; } = null!;

    public ICollection<SupplierBid> SupplierBids { get; set; } = new List<SupplierBid>();
    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
}
