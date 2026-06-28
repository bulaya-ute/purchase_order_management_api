using PurchaseOrderManagement.Api.Dtos.Common;

namespace PurchaseOrderManagement.Api.Dtos.Users;

public class UserListQuery : PagedQuery
{
    /// <summary>Optional filter: only users belonging to this company.</summary>
    public int? CompanyId { get; set; }
}
