namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Cross-cutting audit columns applied to every table (docs/05-CROSS-CUTTING-CONVENTIONS.md).
/// CreatedByUserId/UpdatedByUserId are nullable to allow system/seed-data rows with no acting user.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    int? CreatedByUserId { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    int? UpdatedByUserId { get; set; }
}
