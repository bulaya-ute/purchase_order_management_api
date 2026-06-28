using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Roles;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    // Reads: any authenticated user. Flat list, each with ParentRoleId for the client to build the tree.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _roleService.ListAsync(cancellationToken));
    }

    // Roles the current user may set as a parent (their seniority ceiling + descendants) — UI picker.
    [HttpGet("allowed-parents")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> AllowedParents(CancellationToken cancellationToken)
    {
        return Ok(await _roleService.GetAllowedParentsAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> Get(int id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetAsync(id, cancellationToken);
        return role is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Role {id} was not found.")
            : Ok(role);
    }

    // Create: governed by the seniority-ceiling rule (not the admin-tier check) — enforced in the service.
    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var created = await _roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    // Rename only. System roles are protected (rejected in the service).
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleDto>> Update(int id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _roleService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
