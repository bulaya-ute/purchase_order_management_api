using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Shared recompute logic for PurchaseOrder aggregates (plan section A — "PO totals" decision).
/// Direct-entry POs (no bids) are always single-currency: the flat Subtotal/TaxAmount/TotalAmount
/// fields stay authoritative, zero behavior change from before this change.
/// Bid-based POs (an awarded bid was copied into PurchaseOrderLineItems) may span more than one
/// currency: when line items use more than one currency, the flat fields are zeroed and the
/// authoritative numbers live in PurchaseOrderCurrencyTotals instead (one row per currency,
/// never converted/combined). When a bid-based PO's items happen to all share one currency, the
/// flat fields are populated too (for convenience) alongside the (single-row) vector.
/// </summary>
public static class PurchaseOrderTotalsRecompute
{
    public static async Task RecomputeAsync(AppDbContext db, PurchaseOrder po, bool isBidBased, CancellationToken cancellationToken)
    {
        var perCurrency = await db.PurchaseOrderLineItems
            .Where(li => li.PurchaseOrderId == po.Id)
            .GroupBy(li => li.CurrencyCode)
            .Select(g => new
            {
                CurrencyCode = g.Key,
                Subtotal = g.Sum(li => li.LineSubtotal),
                TaxAmount = g.Sum(li => li.TaxAmount),
                TotalAmount = g.Sum(li => li.LineTotal),
            })
            .ToListAsync(cancellationToken);

        // Replace the existing currency-total rows for this PO wholesale — simplest correct
        // approach given line items can be added/edited/removed freely while in Draft.
        var existingTotals = await db.PurchaseOrderCurrencyTotals
            .Where(t => t.PurchaseOrderId == po.Id)
            .ToListAsync(cancellationToken);
        db.PurchaseOrderCurrencyTotals.RemoveRange(existingTotals);

        var isMultiCurrency = isBidBased && perCurrency.Count > 1;

        if (isBidBased)
        {
            foreach (var row in perCurrency)
            {
                db.PurchaseOrderCurrencyTotals.Add(new PurchaseOrderCurrencyTotal
                {
                    PurchaseOrderId = po.Id,
                    CurrencyCode = row.CurrencyCode,
                    Subtotal = row.Subtotal,
                    TaxAmount = row.TaxAmount,
                    TotalAmount = row.TotalAmount,
                });
            }
        }

        if (isMultiCurrency)
        {
            // Flat fields are meaningless across multiple currencies — zero them, the vector is authoritative.
            po.Subtotal = 0m;
            po.TaxAmount = 0m;
            po.TotalAmount = 0m;
        }
        else
        {
            po.Subtotal = perCurrency.Sum(r => r.Subtotal);
            po.TaxAmount = perCurrency.Sum(r => r.TaxAmount);
            po.TotalAmount = perCurrency.Sum(r => r.TotalAmount);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
