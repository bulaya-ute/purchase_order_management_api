using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.Companies;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly IAdminAuthorizer _adminAuthorizer;

    public CompaniesController(ICompanyService companyService, IAdminAuthorizer adminAuthorizer)
    {
        _companyService = companyService;
        _adminAuthorizer = adminAuthorizer;
    }

    // Reads: any authenticated user.
    [HttpGet]
    public async Task<ActionResult<PagedResult<CompanyDto>>> List([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _companyService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompanyDto>> Get(int id, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetAsync(id, cancellationToken);
        return company is null ? NotFoundProblem(id) : Ok(company);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var created = await _companyService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CompanyDto>> Update(int id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();
        return Ok(await _companyService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        RequireAdmin();
        await _companyService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private void RequireAdmin()
    {
        if (!_adminAuthorizer.IsAdmin())
        {
            throw ServiceException.Forbidden("Company management requires an admin-tier role.");
        }
    }

    private ActionResult NotFoundProblem(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Company {id} was not found.");
}
