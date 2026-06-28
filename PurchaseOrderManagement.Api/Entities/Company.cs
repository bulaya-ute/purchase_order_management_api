namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Represents both the parent company and each child company — all rows in one table,
/// related via a self-referencing FK. See docs/01-IDENTITY-AND-ROLES.md.
/// </summary>
public class Company : BaseEntity
{
    public string Name { get; set; } = null!;

    /// <summary>Null for the top-level/parent company. Child companies point to it.</summary>
    public int? ParentCompanyId { get; set; }
    public Company? ParentCompany { get; set; }

    public ICollection<Company> ChildCompanies { get; set; } = new List<Company>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
