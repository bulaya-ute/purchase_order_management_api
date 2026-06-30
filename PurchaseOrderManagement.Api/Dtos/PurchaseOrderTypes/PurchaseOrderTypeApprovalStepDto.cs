namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrderTypes;

public class PurchaseOrderTypeApprovalStepDto
{
    public int Id { get; set; }
    public int? RequiredRoleId { get; set; }
    public string? RequiredRoleName { get; set; }
    public int? RequiredUserId { get; set; }
    public string? RequiredUserName { get; set; }
    public int SequenceOrder { get; set; }
}
