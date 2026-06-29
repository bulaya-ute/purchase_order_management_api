namespace PurchaseOrderManagement.Api.Dtos.Suppliers;

public class SupplierDto
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Address { get; set; } = null!;
}
