using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Quotations;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// Quotations live under a supplier bid (docs/02). FileId is mandatory; captured line items are
// immutable (re-upload a new quotation rather than editing). Authz: reads + mutations = any
// authenticated user (flagged assumption pending Q7).
[ApiController]
[Route("api/supplier-bids/{supplierBidId:int}/quotations")]
[Authorize]
public class QuotationsController : ControllerBase
{
    private readonly IQuotationService _quotationService;

    public QuotationsController(IQuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuotationSummaryDto>>> List(int supplierBidId, CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.ListForBidAsync(supplierBidId, cancellationToken));
    }

    [HttpGet("{quotationId:int}")]
    public async Task<ActionResult<QuotationDto>> Get(int supplierBidId, int quotationId, CancellationToken cancellationToken)
    {
        var quotation = await _quotationService.GetAsync(supplierBidId, quotationId, cancellationToken);
        return quotation is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Quotation {quotationId} was not found for supplier bid {supplierBidId}.")
            : Ok(quotation);
    }

    [HttpPost]
    public async Task<ActionResult<QuotationDto>> Create(int supplierBidId, [FromBody] CreateQuotationRequest request, CancellationToken cancellationToken)
    {
        var created = await _quotationService.CreateAsync(supplierBidId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { supplierBidId, quotationId = created.Id }, created);
    }
}
