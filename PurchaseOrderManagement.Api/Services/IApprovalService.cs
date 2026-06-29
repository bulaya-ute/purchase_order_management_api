using PurchaseOrderManagement.Api.Dtos.Approvals;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Approval-actor-facing operations: the current user's inbox, and approve/reject actions
/// (docs/04). PO-scoped composition of approval definitions lives on IPurchaseOrderService.
/// </summary>
public interface IApprovalService
{
    Task<IReadOnlyList<MyApprovalDto>> GetMyInboxAsync(CancellationToken cancellationToken);
    Task<ApprovalDto> ApproveAsync(int approvalId, ActOnApprovalRequest request, CancellationToken cancellationToken);
    Task<ApprovalDto> RejectAsync(int approvalId, ActOnApprovalRequest request, CancellationToken cancellationToken);
}
