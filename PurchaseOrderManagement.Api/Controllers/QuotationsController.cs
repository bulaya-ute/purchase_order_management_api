using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Quotations;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// Quotations are a standalone library (plan section A): obtained from suppliers before any
// bid/PO exists, kept for audit even if never used. FileId is mandatory; captured line items are
// immutable (re-upload a new quotation rather than editing). Authz: reads + mutations = any
// authenticated user (flagged assumption pending Q7).
[ApiController]
[Route("api/quotations")]
[Authorize]
public class QuotationsController : ControllerBase
{
    private readonly IQuotationService _quotationService;

    public QuotationsController(IQuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuotationSummaryDto>>> List(
        [FromQuery] int? supplierId, [FromQuery] bool? isExpired, [FromQuery] bool? isUsed, CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.ListAsync(supplierId, isExpired, isUsed, cancellationToken));
    }

    [HttpGet("{quotationId:int}")]
    public async Task<ActionResult<QuotationDto>> Get(int quotationId, CancellationToken cancellationToken)
    {
        var quotation = await _quotationService.GetAsync(quotationId, cancellationToken);
        return quotation is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Quotation {quotationId} was not found.")
            : Ok(quotation);
    }

    [HttpPost]
    public async Task<ActionResult<QuotationDto>> Create([FromBody] CreateQuotationRequest request, CancellationToken cancellationToken)
    {
        var created = await _quotationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { quotationId = created.Id }, created);
    }
}
