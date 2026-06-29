namespace PurchaseOrderManagement.Api.Dtos.Approvals;

/// <summary>
/// One row in the current user's approval inbox (GET /api/approvals/mine): approvals the user
/// can act on right now — Status=Pending, eligible by role/user, and unblocked by sequence
/// gating (docs/04).
/// </summary>
public class MyApprovalDto
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public string PONumber { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = null!;

    public int? RequiredRoleId { get; set; }
    public string? RequiredRoleName { get; set; }
    public int? RequiredUserId { get; set; }

    public int SequenceOrder { get; set; }

    /// <summary>Base64-encoded xmin concurrency token for the approval row.</summary>
    public string RowVersion { get; set; } = null!;
}
