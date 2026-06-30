namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Many-to-many: roles allowed to create a PO of a given PurchaseOrderType. The creator must hold
/// at least one of these roles, else PurchaseOrderService.CreateAsync rejects with Forbidden.
/// </summary>
public class PurchaseOrderTypeAllowedCreatorRole : BaseEntity
{
    public int PurchaseOrderTypeId { get; set; }
    public PurchaseOrderType PurchaseOrderType { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
