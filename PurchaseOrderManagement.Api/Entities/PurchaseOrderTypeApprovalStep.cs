namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// One template step in a PurchaseOrderType's fixed approval chain. Exactly one of
/// RequiredRoleId / RequiredUserId must be non-null (DB check constraint) — same XOR shape as
/// <see cref="Approval"/>. Copied 1:1 into real <see cref="Approval"/> rows when a PO of this type
/// is created; the creator cannot edit/bypass this chain (PurchaseOrderService rejects
/// AddApprovalDefinitionAsync/RemoveApprovalDefinitionAsync for typed POs).
/// </summary>
public class PurchaseOrderTypeApprovalStep : BaseEntity
{
    public int PurchaseOrderTypeId { get; set; }
    public PurchaseOrderType PurchaseOrderType { get; set; } = null!;

    /// <summary>If set, any user holding this role may act on the generated Approval.</summary>
    public int? RequiredRoleId { get; set; }
    public Role? RequiredRole { get; set; }

    /// <summary>If set, only this specific user may act on the generated Approval.</summary>
    public int? RequiredUserId { get; set; }
    public User? RequiredUser { get; set; }

    public int SequenceOrder { get; set; }
}
