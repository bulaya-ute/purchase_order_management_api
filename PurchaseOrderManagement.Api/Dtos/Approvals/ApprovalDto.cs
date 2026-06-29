using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Dtos.Approvals;

public class ApprovalDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }

    public int? RequiredRoleId { get; set; }
    public string? RequiredRoleName { get; set; }

    public int? RequiredUserId { get; set; }
    public string? RequiredUserName { get; set; }

    public int SequenceOrder { get; set; }
    public ApprovalStatus Status { get; set; }

    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? Comment { get; set; }

    /// <summary>Base64-encoded xmin concurrency token for the approval row.</summary>
    public string RowVersion { get; set; } = null!;
}
