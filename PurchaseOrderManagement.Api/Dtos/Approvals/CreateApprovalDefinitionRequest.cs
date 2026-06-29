using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Approvals;

/// <summary>
/// Defines a required approval row while the PO is still Draft. Exactly one of RequiredRoleId /
/// RequiredUserId must be supplied (DB check constraint mirrors this — docs/04).
/// </summary>
public class CreateApprovalDefinitionRequest
{
    public int? RequiredRoleId { get; set; }

    public int? RequiredUserId { get; set; }

    [Range(0, int.MaxValue)]
    public int SequenceOrder { get; set; } = 0;
}
