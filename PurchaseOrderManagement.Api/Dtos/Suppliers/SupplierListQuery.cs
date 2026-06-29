using PurchaseOrderManagement.Api.Dtos.Common;

namespace PurchaseOrderManagement.Api.Dtos.Suppliers;

public class SupplierListQuery : PagedQuery
{
    /// <summary>Optional case-insensitive filter on SupplierName.</summary>
    public string? Search { get; set; }
}
