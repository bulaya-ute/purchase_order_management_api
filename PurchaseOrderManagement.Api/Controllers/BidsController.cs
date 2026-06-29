using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.SupplierBids;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// Supplier bids + bid items — the comparison-ready procurement data (docs/02, docs/07).
// Bids are created/listed under a PO; individual bids and their items are addressed directly.
// Awarding and copying items into PurchaseOrderLineItems are intentionally NOT here (PO core slice).
// Authz: reads + mutations = any authenticated user (flagged assumption pending Q7).
[ApiController]
[Authorize]
public class BidsController : ControllerBase
{
    private readonly IBidService _bidService;

    public BidsController(IBidService bidService)
    {
        _bidService = bidService;
    }

    // ----- Bids scoped to a purchase order -----

    [HttpGet("api/purchase-orders/{purchaseOrderId:int}/bids")]
    public async Task<ActionResult<IReadOnlyList<SupplierBidSummaryDto>>> ListForPurchaseOrder(int purchaseOrderId, CancellationToken cancellationToken)
    {
        return Ok(await _bidService.ListForPurchaseOrderAsync(purchaseOrderId, cancellationToken));
    }

    [HttpPost("api/purchase-orders/{purchaseOrderId:int}/bids")]
    public async Task<ActionResult<SupplierBidDto>> Create(int purchaseOrderId, [FromBody] CreateSupplierBidRequest request, CancellationToken cancellationToken)
    {
        var created = await _bidService.CreateAsync(purchaseOrderId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    // ----- Individual bid -----

    [HttpGet("api/supplier-bids/{id:int}")]
    public async Task<ActionResult<SupplierBidDto>> Get(int id, CancellationToken cancellationToken)
    {
        var bid = await _bidService.GetAsync(id, cancellationToken);
        return bid is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Supplier bid {id} was not found.")
            : Ok(bid);
    }

    // ----- Bid items -----

    [HttpPost("api/supplier-bids/{supplierBidId:int}/items")]
    public async Task<ActionResult<SupplierBidItemDto>> AddItem(int supplierBidId, [FromBody] CreateSupplierBidItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bidService.AddItemAsync(supplierBidId, request, cancellationToken));
    }

    [HttpPut("api/supplier-bids/{supplierBidId:int}/items/{itemId:int}")]
    public async Task<ActionResult<SupplierBidItemDto>> UpdateItem(int supplierBidId, int itemId, [FromBody] UpdateSupplierBidItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bidService.UpdateItemAsync(supplierBidId, itemId, request, cancellationToken));
    }

    [HttpDelete("api/supplier-bids/{supplierBidId:int}/items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int supplierBidId, int itemId, CancellationToken cancellationToken)
    {
        await _bidService.RemoveItemAsync(supplierBidId, itemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/supplier-bids/{supplierBidId:int}/items/seed-from-quotation")]
    public async Task<ActionResult<SupplierBidDto>> SeedItemsFromQuotation(int supplierBidId, [FromBody] SeedBidItemsFromQuotationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _bidService.SeedItemsFromQuotationAsync(supplierBidId, request, cancellationToken));
    }
}
