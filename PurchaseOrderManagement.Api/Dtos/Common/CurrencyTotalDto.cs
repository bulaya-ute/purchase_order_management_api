namespace PurchaseOrderManagement.Api.Dtos.Common;

/// <summary>
/// One currency's aggregate within a money vector (SupplierBidDto.Totals, PurchaseOrderDto.Totals).
/// Never combined/converted across currencies — one row per currency present among the
/// underlying line items.
/// </summary>
public class CurrencyTotalDto
{
    public string Currency { get; set; } = null!;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
