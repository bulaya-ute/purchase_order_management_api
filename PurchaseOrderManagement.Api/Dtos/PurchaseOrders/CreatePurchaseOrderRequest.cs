using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

/// <summary>
/// Creates a new PO in Draft. IssuerUserId is taken from the current user, never from the
/// client. PONumber is server-generated after the row exists (docs/03).
/// </summary>
public class CreatePurchaseOrderRequest
{
    [Required]
    public int CompanyId { get; set; }

    /// <summary>Who the purchase is for (a branch/company), distinct from CompanyId. Optional.</summary>
    public int? TargetCompanyId { get; set; }

    /// <summary>
    /// ISO 4217 code (e.g. ZMW, USD, KES). Optional — defaults to "ZMW" server-side when omitted.
    /// Must reference an active Currency row.
    /// </summary>
    [StringLength(3, MinimumLength = 3)]
    public string? Currency { get; set; }

    /// <summary>Optional: creates the PO from an admin-defined type preset (fixed approval chain, restricted creator roles).</summary>
    public int? PurchaseOrderTypeId { get; set; }

    [StringLength(2048)]
    public string? Notes { get; set; }
}
