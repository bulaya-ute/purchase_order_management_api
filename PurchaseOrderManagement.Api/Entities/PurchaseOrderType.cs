namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// An admin-defined PO preset: a fixed, immutable approval chain (PurchaseOrderTypeApprovalStep)
/// plus a restriction on which roles may create a PO of this type
/// (PurchaseOrderTypeAllowedCreatorRole). Line items remain unconstrained by type.
/// See plan section C.
/// </summary>
public class PurchaseOrderType : BaseEntity
{
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public ICollection<PurchaseOrderTypeApprovalStep> ApprovalSteps { get; set; } = new List<PurchaseOrderTypeApprovalStep>();
    public ICollection<PurchaseOrderTypeAllowedCreatorRole> AllowedCreatorRoles { get; set; } = new List<PurchaseOrderTypeAllowedCreatorRole>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
