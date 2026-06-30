using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Currencies;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Admin-managed currency reference data (plan section A/D). Used to validate every Currency
/// reference (Quotation, SupplierBidItem, PurchaseOrder, PurchaseOrderLineItem) against active
/// rows here instead of a hardcoded allowlist.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly AppDbContext _db;
    private readonly IAdminAuthorizer _adminAuthorizer;

    public CurrencyService(AppDbContext db, IAdminAuthorizer adminAuthorizer)
    {
        _db = db;
        _adminAuthorizer = adminAuthorizer;
    }

    public async Task<IReadOnlyList<CurrencyDto>> ListAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var query = _db.Currencies.AsNoTracking().AsQueryable();

        if (isActive is bool active)
        {
            query = query.Where(c => c.IsActive == active);
        }

        return await query
            .OrderBy(c => c.Code)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<CurrencyDto?> GetAsync(string code, CancellationToken cancellationToken)
    {
        return await _db.Currencies.AsNoTracking()
            .Where(c => c.Code == NormalizeCode(code))
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();

        var code = NormalizeCode(request.Code);

        var exists = await _db.Currencies.AnyAsync(c => c.Code == code, cancellationToken);
        if (exists)
        {
            throw ServiceException.Conflict($"Currency {code} already exists.");
        }

        var currency = new Currency
        {
            Code = code,
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
        };

        _db.Currencies.Add(currency);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(code, cancellationToken))!;
    }

    public async Task<CurrencyDto> UpdateAsync(string code, UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();

        var normalizedCode = NormalizeCode(code);

        var currency = await _db.Currencies.FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken)
            ?? throw ServiceException.NotFound($"Currency {normalizedCode} was not found.");

        currency.Name = request.Name.Trim();
        currency.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(normalizedCode, cancellationToken))!;
    }

    private void RequireAdmin()
    {
        if (!_adminAuthorizer.IsAdmin())
        {
            throw ServiceException.Forbidden("Currency management requires an admin-tier role.");
        }
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static readonly System.Linq.Expressions.Expression<Func<Currency, CurrencyDto>> ToDto =
        c => new CurrencyDto
        {
            Code = c.Code,
            Name = c.Name,
            IsActive = c.IsActive,
        };
}
