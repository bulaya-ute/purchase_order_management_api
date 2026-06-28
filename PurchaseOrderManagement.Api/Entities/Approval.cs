using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// A row's existence means an approval is required for the PO — not that it's been granted.
/// Exactly one of RequiredRoleId / RequiredUserId must be non-null (DB check constraint).
/// See docs/04-APPROVALS.md.
/// </summary>
public class Approval : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>If set, any user holding this role may act on it.</summary>
    public int? RequiredRoleId { get; set; }
    public Role? RequiredRole { get; set; }

    /// <summary>If set, only this specific user may act on it.</summary>
    public int? RequiredUserId { get; set; }
    public User? RequiredUser { get; set; }

    public int SequenceOrder { get; set; }

    public ApprovalStatus Status { get; set; }

    /// <summary>The actual user who acted — relevant when RequiredRoleId is set.</summary>
    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public string? Comment { get; set; }
}
