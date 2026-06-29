using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.Suppliers;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// Global, shared across all companies in the group (docs/02-SUPPLIERS-AND-PROCUREMENT.md).
// Authz: reads = any authenticated user; mutations = any authenticated user — procurement is a
// normal function, not admin-only. Flagged assumption pending the open Q7 authz decision.
[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierDto>>> List([FromQuery] SupplierListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierDto>> Get(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetAsync(id, cancellationToken);
        return supplier is null ? NotFoundProblem(id) : Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var created = await _supplierService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierDto>> Update(int id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _supplierService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private ActionResult NotFoundProblem(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Supplier {id} was not found.");
}
