using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Shared currency-code validation used by every service that accepts a Currency field
/// (Quotation, SupplierBidItem, PurchaseOrder, PurchaseOrderLineItem — plan section A/D).
/// Rejects unknown or inactive codes rather than trusting a hardcoded allowlist.
/// </summary>
public static class CurrencyValidation
{
    public const string DefaultCurrencyCode = "ZMW";

    /// <summary>Normalizes (trim+upper) and validates that the code refers to an active Currency row.</summary>
    public static async Task<string> NormalizeAndValidateAsync(AppDbContext db, string code, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();

        var currency = await db.Currencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == normalized, cancellationToken);

        if (currency is null)
        {
            throw ServiceException.Validation($"Currency {normalized} was not found.");
        }

        if (!currency.IsActive)
        {
            throw ServiceException.Validation($"Currency {normalized} is not active.");
        }

        return normalized;
    }
}
