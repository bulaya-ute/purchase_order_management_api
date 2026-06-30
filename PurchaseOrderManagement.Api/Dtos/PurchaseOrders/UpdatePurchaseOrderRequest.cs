using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

/// <summary>
/// Edits header fields on a Draft PO only. Composition (lines/awarded bid/approvals) has its
/// own endpoints; this is Notes/Currency/TargetCompanyId (docs/03).
/// </summary>
public class UpdatePurchaseOrderRequest
{
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = null!;

    /// <summary>Who the purchase is for (a branch/company), distinct from CompanyId. Editable only in Draft.</summary>
    public int? TargetCompanyId { get; set; }

    [StringLength(2048)]
    public string? Notes { get; set; }

    /// <summary>Base64-encoded xmin concurrency token from the last read, if available.</summary>
    public string? RowVersion { get; set; }
}
