namespace PurchaseOrderManagement.Api.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.PurchaseOrder"/>.
/// Draft -> Open -> Approved is the happy path; Rejected and Cancelled are terminal.
/// </summary>
public enum PurchaseOrderStatus
{
    Draft,
    Open,
    Approved,
    Rejected,
    Cancelled
}
