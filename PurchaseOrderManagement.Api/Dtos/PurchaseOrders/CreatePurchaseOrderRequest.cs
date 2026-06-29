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

    /// <summary>ISO 4217 code (e.g. USD, KES, GBP).</summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = null!;

    [StringLength(2048)]
    public string? Notes { get; set; }
}
