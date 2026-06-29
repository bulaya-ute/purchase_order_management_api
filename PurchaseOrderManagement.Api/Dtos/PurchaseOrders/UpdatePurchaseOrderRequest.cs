using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

/// <summary>
/// Edits header fields on a Draft PO only. Composition (lines/awarded bid/approvals) has its
/// own endpoints; this is just Notes/Currency (docs/03).
/// </summary>
public class UpdatePurchaseOrderRequest
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = null!;

    [StringLength(2048)]
    public string? Notes { get; set; }

    /// <summary>Base64-encoded xmin concurrency token from the last read, if available.</summary>
    public string? RowVersion { get; set; }
}
