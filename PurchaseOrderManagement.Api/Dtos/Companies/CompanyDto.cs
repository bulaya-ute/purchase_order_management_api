namespace PurchaseOrderManagement.Api.Dtos.Companies;

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentCompanyId { get; set; }
    public string? ParentCompanyName { get; set; }
}
