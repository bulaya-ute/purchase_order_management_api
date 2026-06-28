namespace PurchaseOrderManagement.Api.Enums;

/// <summary>
/// Status of a single required <see cref="Entities.Approval"/> row for a purchase order.
/// Skipped means the approval was never acted on because the PO was rejected first.
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Skipped
}
