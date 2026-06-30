namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrderTypes;

/// <summary>Exactly one of RequiredRoleId / RequiredUserId must be supplied (validated in the service, mirroring CreateApprovalDefinitionRequest).</summary>
public class CreatePurchaseOrderTypeApprovalStepRequest
{
    public int? RequiredRoleId { get; set; }
    public int? RequiredUserId { get; set; }
    public int SequenceOrder { get; set; }
}
