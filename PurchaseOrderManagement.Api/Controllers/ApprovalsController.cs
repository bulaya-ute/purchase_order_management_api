using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Approvals;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// Approval-actor-facing endpoints (docs/04): the current user's inbox, and approve/reject
// actions. PO-scoped approval definitions/listing live under PurchaseOrdersController.
// Authz: any authenticated user may call these; eligibility (role/user match) and sequence
// gating are enforced in ApprovalService per-approval, not via [Authorize(Roles=...)].
[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<MyApprovalDto>>> Mine(CancellationToken cancellationToken)
    {
        return Ok(await _approvalService.GetMyInboxAsync(cancellationToken));
    }

    [HttpPost("{approvalId:int}/approve")]
    public async Task<ActionResult<ApprovalDto>> Approve(int approvalId, [FromBody] ActOnApprovalRequest? request, CancellationToken cancellationToken)
    {
        return Ok(await _approvalService.ApproveAsync(approvalId, request ?? new ActOnApprovalRequest(), cancellationToken));
    }

    [HttpPost("{approvalId:int}/reject")]
    public async Task<ActionResult<ApprovalDto>> Reject(int approvalId, [FromBody] ActOnApprovalRequest? request, CancellationToken cancellationToken)
    {
        return Ok(await _approvalService.RejectAsync(approvalId, request ?? new ActOnApprovalRequest(), cancellationToken));
    }
}
