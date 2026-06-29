using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Approvals;

public class ActOnApprovalRequest
{
    [StringLength(2048)]
    public string? Comment { get; set; }

    /// <summary>Base64-encoded xmin concurrency token from the last read, if available.</summary>
    public string? RowVersion { get; set; }
}
