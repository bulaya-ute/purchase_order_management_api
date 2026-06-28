namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// Base class providing the int identity PK plus the audit and soft-delete columns
/// that every table in the system carries (docs/05-CROSS-CUTTING-CONVENTIONS.md).
/// </summary>
public abstract class BaseEntity : IAuditableEntity, ISoftDeletableEntity
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public int? DeletedByUserId { get; set; }
}
