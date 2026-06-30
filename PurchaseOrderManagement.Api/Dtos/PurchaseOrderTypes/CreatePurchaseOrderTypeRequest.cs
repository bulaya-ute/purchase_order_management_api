using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrderTypes;

/// <summary>Creates a PO type preset along with its full set of approval steps and allowed creator roles in one call.</summary>
public class CreatePurchaseOrderTypeRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public List<CreatePurchaseOrderTypeApprovalStepRequest> ApprovalSteps { get; set; } = new();

    public List<int> AllowedCreatorRoleIds { get; set; } = new();
}
