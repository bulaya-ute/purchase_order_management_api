namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Per-currency aggregate for a bid-based PO whose awarded bid's line items span more than one
/// currency (never converted/combined). Recomputed whenever PurchaseOrderLineItems change for the
/// PO, same as the flat Subtotal/TaxAmount/TotalAmount fields on PurchaseOrder are for direct-entry
/// POs. One row per currency present among the PO's line items. See docs/03-PURCHASE-ORDERS.md.
/// </summary>
public class PurchaseOrderCurrencyTotal
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public string CurrencyCode { get; set; } = null!;
    public Currency Currency { get; set; } = null!;

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
