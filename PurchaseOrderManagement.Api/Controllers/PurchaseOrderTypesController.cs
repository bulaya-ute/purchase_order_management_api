using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.PurchaseOrderTypes;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// PO type presets (plan section C): fixed approval chains + creator-role restriction. Reads: any
// authenticated user (the PO composer's type picker needs the list). Mutations: admin-tier only,
// enforced in the service via IAdminAuthorizer.RequireAdmin() (mirrors RolesController/RoleService).
[ApiController]
[Route("api/purchase-order-types")]
[Authorize]
public class PurchaseOrderTypesController : ControllerBase
{
    private readonly IPurchaseOrderTypeService _purchaseOrderTypeService;

    public PurchaseOrderTypesController(IPurchaseOrderTypeService purchaseOrderTypeService)
    {
        _purchaseOrderTypeService = purchaseOrderTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderTypeDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderTypeService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderTypeDto>> Get(int id, CancellationToken cancellationToken)
    {
        var type = await _purchaseOrderTypeService.GetAsync(id, cancellationToken);
        return type is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Purchase order type {id} was not found.")
            : Ok(type);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderTypeDto>> Create([FromBody] CreatePurchaseOrderTypeRequest request, CancellationToken cancellationToken)
    {
        var created = await _purchaseOrderTypeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PurchaseOrderTypeDto>> Update(int id, [FromBody] UpdatePurchaseOrderTypeRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _purchaseOrderTypeService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _purchaseOrderTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
