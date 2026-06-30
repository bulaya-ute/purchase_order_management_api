using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrderTypes;

/// <summary>
/// Full replace semantics for the type's name/active flag, approval steps, and allowed creator
/// roles — mirrors how RoleService treats updates as authoritative snapshots rather than deltas.
/// Note: this only affects the *template*; PurchaseOrders that already auto-generated their
/// Approval rows from a previous version of this type are unaffected (immutability is per-PO).
/// </summary>
public class UpdatePurchaseOrderTypeRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public List<CreatePurchaseOrderTypeApprovalStepRequest> ApprovalSteps { get; set; } = new();

    public List<int> AllowedCreatorRoleIds { get; set; } = new();
}
