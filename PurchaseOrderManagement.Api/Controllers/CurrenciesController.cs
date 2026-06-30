using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderManagement.Api.Dtos.Currencies;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Controllers;

// Currency reference data (plan section A/D): admin CRUD; reads available to any authenticated
// user (every currency-bearing form needs the active list). Create/update gated by IAdminAuthorizer.
[ApiController]
[Route("api/currencies")]
[Authorize]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrenciesController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CurrencyDto>>> List([FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        return Ok(await _currencyService.ListAsync(isActive, cancellationToken));
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<CurrencyDto>> Get(string code, CancellationToken cancellationToken)
    {
        var currency = await _currencyService.GetAsync(code, cancellationToken);
        return currency is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: $"Currency {code} was not found.")
            : Ok(currency);
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyDto>> Create([FromBody] CreateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var created = await _currencyService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { code = created.Code }, created);
    }

    [HttpPut("{code}")]
    public async Task<ActionResult<CurrencyDto>> Update(string code, [FromBody] UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _currencyService.UpdateAsync(code, request, cancellationToken));
    }
}
