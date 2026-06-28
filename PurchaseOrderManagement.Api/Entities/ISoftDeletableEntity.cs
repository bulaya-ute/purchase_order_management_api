namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Cross-cutting soft-delete columns applied to every table (docs/05-CROSS-CUTTING-CONVENTIONS.md).
/// Nothing is ever hard-deleted; a global EF query filter excludes IsDeleted rows everywhere.
/// </summary>
public interface ISoftDeletableEntity
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    int? DeletedByUserId { get; set; }
}
