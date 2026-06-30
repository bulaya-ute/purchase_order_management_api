namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Admin-managed currency reference data. Code is the natural key (ISO 4217, char(3)).
/// All currency-bearing fields (PurchaseOrder, Quotation, SupplierBidItem, PurchaseOrderLineItem)
/// validate against active rows here instead of a hardcoded allowlist. Seeded with ZMW (active).
/// </summary>
public class Currency
{
    /// <summary>ISO 4217 code, e.g. "ZMW". Primary key.</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }
}
