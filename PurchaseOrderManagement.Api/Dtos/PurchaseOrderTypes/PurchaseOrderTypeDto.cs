namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrderTypes;

public class PurchaseOrderTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }

    public IReadOnlyList<PurchaseOrderTypeApprovalStepDto> ApprovalSteps { get; set; } = Array.Empty<PurchaseOrderTypeApprovalStepDto>();

    public IReadOnlyList<int> AllowedCreatorRoleIds { get; set; } = Array.Empty<int>();
    public IReadOnlyList<string> AllowedCreatorRoleNames { get; set; } = Array.Empty<string>();
}
