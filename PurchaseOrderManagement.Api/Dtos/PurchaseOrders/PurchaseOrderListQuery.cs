using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

/// <summary>
/// List filters for GET /api/purchase-orders. Defaults to scoping results to the current
/// user's CompanyId unless an explicit CompanyId filter is supplied (docs/08 [DECIDE] —
/// flagged assumption, see PurchaseOrderService remarks).
/// </summary>
public class PurchaseOrderListQuery : PagedQuery
{
    public PurchaseOrderStatus? Status { get; set; }

    /// <summary>Optional explicit company filter. If omitted, defaults to the current user's CompanyId.</summary>
    public int? CompanyId { get; set; }
}
