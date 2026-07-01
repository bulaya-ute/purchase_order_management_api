using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Approvals;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.PurchaseOrders;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// Purchase orders: header CRUD (Draft only for edits), composition (direct-entry lines, awarded
// bid selection, approval definitions — all Draft only), submit, milestones, and cancel (docs/03).
// Authz: any authenticated user may create/compose POs; company-scoped by default on the list
// endpoint. Flagged assumption pending the open Q7 authz decision (docs/08).
[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderSummaryDto>>> List([FromQuery] PurchaseOrderListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> Get(int id, CancellationToken cancellationToken)
    {
        var po = await _purchaseOrderService.GetAsync(id, cancellationToken);
        return po is null ? NotFoundProblem(id) : Ok(po);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var created = await _purchaseOrderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> Update(int id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.UpdateAsync(id, request, cancellationToken));
    }

    // ----- Composition: direct-entry line items (Draft only) -----

    [HttpPost("{id:int}/line-items")]
    public async Task<ActionResult<PurchaseOrderLineItemDto>> AddLineItem(int id, [FromBody] CreatePurchaseOrderLineItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.AddLineItemAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:int}/line-items/{lineItemId:int}")]
    public async Task<ActionResult<PurchaseOrderLineItemDto>> UpdateLineItem(int id, int lineItemId, [FromBody] UpdatePurchaseOrderLineItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.UpdateLineItemAsync(id, lineItemId, request, cancellationToken));
    }

    [HttpDelete("{id:int}/line-items/{lineItemId:int}")]
    public async Task<IActionResult> RemoveLineItem(int id, int lineItemId, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.RemoveLineItemAsync(id, lineItemId, cancellationToken);
        return NoContent();
    }

    // ----- Composition: Supplier Bid attachment (Draft only, lock on primary) -----

    [HttpPost("{id:int}/supplier-bids")]
    public async Task<IActionResult> AttachSupplierBid(int id, [FromBody] AttachSupplierBidRequest request, CancellationToken cancellationToken)
    {
        var dto = await _purchaseOrderService.AttachSupplierBidAsync(id, request.SupplierBidId, request.IsPrimary, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:int}/supplier-bids/{supplierBidId:int}")]
    public async Task<IActionResult> DetachSupplierBid(int id, int supplierBidId, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.DetachSupplierBidAsync(id, supplierBidId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/supplier-bids/{supplierBidId:int}/set-primary")]
    public async Task<IActionResult> SetPrimarySupplierBid(int id, int supplierBidId, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.SetPrimarySupplierBidAsync(id, supplierBidId, cancellationToken);
        return NoContent();
    }

    // ----- Composition: awarded bid selection (Draft only) -----

    [HttpPost("{id:int}/awarded-bid")]
    public async Task<ActionResult<PurchaseOrderDto>> SelectAwardedBid(int id, [FromBody] SelectAwardedBidRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.SelectAwardedBidAsync(id, request, cancellationToken));
    }

    // ----- Composition: approval definitions (Draft only) -----

    [HttpPost("{id:int}/approvals")]
    public async Task<ActionResult<ApprovalDto>> AddApprovalDefinition(int id, [FromBody] CreateApprovalDefinitionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.AddApprovalDefinitionAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}/approvals/{approvalId:int}")]
    public async Task<IActionResult> RemoveApprovalDefinition(int id, int approvalId, CancellationToken cancellationToken)
    {
        await _purchaseOrderService.RemoveApprovalDefinitionAsync(id, approvalId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/approvals")]
    public async Task<ActionResult<IReadOnlyList<ApprovalDto>>> ListApprovals(int id, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.ListApprovalsAsync(id, cancellationToken));
    }

    // ----- Lifecycle -----

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<PurchaseOrderDto>> Submit(int id, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.SubmitAsync(id, cancellationToken));
    }

    [HttpPost("{id:int}/pay")]
    public async Task<ActionResult<PurchaseOrderDto>> Pay(int id, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.PayAsync(id, cancellationToken));
    }

    [HttpPost("{id:int}/deliver")]
    public async Task<ActionResult<PurchaseOrderDto>> Deliver(int id, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.DeliverAsync(id, cancellationToken));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderService.CancelAsync(id, cancellationToken));
    }

    private ActionResult NotFoundProblem(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Purchase order {id} was not found.");
}
