using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Server-side money computation shared by SupplierBidItems (docs/02) and PurchaseOrderLineItems
/// (docs/03) — both have the identical Quantity/UnitCost/DiscountPercentage?/TaxPercentage? ->
/// LineSubtotal/DiscountAmount/TaxAmount/LineTotal shape. Never trust client-supplied computed
/// amounts: they are always derived from the editable inputs and rounded to 2dp here.
/// </summary>
public static class BidItemMath
{
    public static void Apply(SupplierBidItem item)
    {
        var (lineSubtotal, discountAmount, taxAmount, lineTotal) =
            Compute(item.Quantity, item.UnitCost, item.DiscountPercentage, item.TaxPercentage);

        item.LineSubtotal = lineSubtotal;
        item.DiscountAmount = discountAmount;
        item.TaxAmount = taxAmount;
        item.LineTotal = lineTotal;
    }

    public static void Apply(PurchaseOrderLineItem item)
    {
        var (lineSubtotal, discountAmount, taxAmount, lineTotal) =
            Compute(item.Quantity, item.UnitCost, item.DiscountPercentage, item.TaxPercentage);

        item.LineSubtotal = lineSubtotal;
        item.DiscountAmount = discountAmount;
        item.TaxAmount = taxAmount;
        item.LineTotal = lineTotal;
    }

    private static (decimal LineSubtotal, decimal DiscountAmount, decimal TaxAmount, decimal LineTotal) Compute(
        decimal quantity, decimal unitCost, decimal? discountPercentage, decimal? taxPercentage)
    {
        var lineSubtotal = Round(quantity * unitCost);

        var discountAmount = discountPercentage is decimal discountPct
            ? Round(lineSubtotal * (discountPct / 100m))
            : 0m;

        var taxableBase = lineSubtotal - discountAmount;

        var taxAmount = taxPercentage is decimal taxPct
            ? Round(taxableBase * (taxPct / 100m))
            : 0m;

        var lineTotal = Round(lineSubtotal - discountAmount + taxAmount);

        return (lineSubtotal, discountAmount, taxAmount, lineTotal);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
