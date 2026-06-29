using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Server-side money computation for SupplierBidItems (docs/02). Never trust client-supplied
/// computed amounts: LineSubtotal/DiscountAmount/TaxAmount/LineTotal are derived from the
/// editable inputs (Quantity, UnitCost, DiscountPercentage?, TaxPercentage?) and rounded to 2dp.
/// </summary>
public static class BidItemMath
{
    public static void Apply(SupplierBidItem item)
    {
        var lineSubtotal = Round(item.Quantity * item.UnitCost);

        var discountAmount = item.DiscountPercentage is decimal discountPct
            ? Round(lineSubtotal * (discountPct / 100m))
            : 0m;

        var taxableBase = lineSubtotal - discountAmount;

        var taxAmount = item.TaxPercentage is decimal taxPct
            ? Round(taxableBase * (taxPct / 100m))
            : 0m;

        item.LineSubtotal = lineSubtotal;
        item.DiscountAmount = discountAmount;
        item.TaxAmount = taxAmount;
        item.LineTotal = Round(lineSubtotal - discountAmount + taxAmount);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
