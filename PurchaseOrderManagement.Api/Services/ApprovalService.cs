using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Approvals;
using PurchaseOrderManagement.Api.Entities;
using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Approval-actor-facing operations (docs/04): the current user's inbox, and the approve/reject
/// actions that drive PO-completion (bid-based line copy) and rejection-cascade (Skipped flip).
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ApprovalService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MyApprovalDto>> GetMyInboxAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("An authenticated user is required.");

        var roleIds = await GetCurrentUserRoleIdsAsync(userId, cancellationToken);

        // Candidate set: Pending approvals the user is eligible for by role or by user id.
        var candidates = await _db.Approvals
            .Include(a => a.RequiredRole)
            .Include(a => a.PurchaseOrder).ThenInclude(po => po.Company)
            .Where(a => a.Status == ApprovalStatus.Pending)
            .Where(a => (a.RequiredRoleId != null && roleIds.Contains(a.RequiredRoleId.Value))
                        || a.RequiredUserId == userId)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return Array.Empty<MyApprovalDto>();
        }

        var poIds = candidates.Select(a => a.PurchaseOrderId).Distinct().ToList();

        // For sequence gating we need, per PO, whether any approval at a lower SequenceOrder
        // is still Pending. Load all approvals for the candidate POs once.
        var allApprovalsByPo = await _db.Approvals
            .Where(a => poIds.Contains(a.PurchaseOrderId))
            .Select(a => new { a.PurchaseOrderId, a.SequenceOrder, a.Status })
            .ToListAsync(cancellationToken);

        var actionable = candidates.Where(candidate =>
        {
            var blockedByLowerSequence = allApprovalsByPo.Any(a =>
                a.PurchaseOrderId == candidate.PurchaseOrderId
                && a.SequenceOrder < candidate.SequenceOrder
                && a.Status == ApprovalStatus.Pending);

            return !blockedByLowerSequence;
        }).ToList();

        return actionable
            .OrderBy(a => a.PurchaseOrderId).ThenBy(a => a.SequenceOrder).ThenBy(a => a.Id)
            .Select(a => new MyApprovalDto
            {
                Id = a.Id,
                PurchaseOrderId = a.PurchaseOrderId,
                PONumber = a.PurchaseOrder.PONumber,
                CompanyName = a.PurchaseOrder.Company.Name,
                TotalAmount = a.PurchaseOrder.TotalAmount,
                Currency = a.PurchaseOrder.CurrencyCode,
                RequiredRoleId = a.RequiredRoleId,
                RequiredRoleName = a.RequiredRole?.Name,
                RequiredUserId = a.RequiredUserId,
                SequenceOrder = a.SequenceOrder,
                RowVersion = ConcurrencyToken.Encode(_db.Entry(a).Property<uint>("xmin").CurrentValue),
            })
            .ToList();
    }

    public async Task<ApprovalDto> ApproveAsync(int approvalId, ActOnApprovalRequest request, CancellationToken cancellationToken)
    {
        var approval = await LoadTrackedApprovalAsync(approvalId, cancellationToken);
        var userId = await EnsureEligibleAndUnblockedAsync(approval, cancellationToken);

        ApplyConcurrencyToken(approval, request.RowVersion);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        approval.Status = ApprovalStatus.Approved;
        approval.ApprovedByUserId = userId;
        approval.ApprovedAtUtc = DateTime.UtcNow;
        approval.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        await SaveWithConcurrencyAsync(cancellationToken);

        var fullyApproved = await IsFullyApprovedAsync(approval.PurchaseOrderId, cancellationToken);
        if (fullyApproved)
        {
            await CompletePurchaseOrderApprovalAsync(approval.PurchaseOrderId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return await ToApprovalDtoAsync(approval.Id, cancellationToken);
    }

    public async Task<ApprovalDto> RejectAsync(int approvalId, ActOnApprovalRequest request, CancellationToken cancellationToken)
    {
        var approval = await LoadTrackedApprovalAsync(approvalId, cancellationToken);
        var userId = await EnsureEligibleAndUnblockedAsync(approval, cancellationToken);

        ApplyConcurrencyToken(approval, request.RowVersion);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        approval.Status = ApprovalStatus.Rejected;
        approval.ApprovedByUserId = userId;
        approval.ApprovedAtUtc = DateTime.UtcNow;
        approval.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        await SaveWithConcurrencyAsync(cancellationToken);

        // Rejection rule (docs/04): the whole PO immediately dies, and every other still-Pending
        // approval on it is flipped to Skipped in the same operation.
        var po = await _db.PurchaseOrders.FirstAsync(p => p.Id == approval.PurchaseOrderId, cancellationToken);
        po.Status = PurchaseOrderStatus.Rejected;

        var stillPending = await _db.Approvals
            .Where(a => a.PurchaseOrderId == approval.PurchaseOrderId && a.Id != approval.Id && a.Status == ApprovalStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var pending in stillPending)
        {
            pending.Status = ApprovalStatus.Skipped;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await ToApprovalDtoAsync(approval.Id, cancellationToken);
    }

    // ----- Eligibility / sequence gating -----

    private async Task<int> EnsureEligibleAndUnblockedAsync(Approval approval, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("An authenticated user is required.");

        if (approval.Status != ApprovalStatus.Pending)
        {
            throw ServiceException.Conflict($"Approval {approval.Id} is not Pending (current status: {approval.Status}).");
        }

        var eligible = false;

        if (approval.RequiredUserId is int requiredUserId)
        {
            // Self-approval is allowed (docs/04, Q13 resolved) — no extra check needed beyond
            // "is this the designated user".
            eligible = requiredUserId == userId;
        }
        else if (approval.RequiredRoleId is int requiredRoleId)
        {
            var roleIds = await GetCurrentUserRoleIdsAsync(userId, cancellationToken);
            eligible = roleIds.Contains(requiredRoleId);
        }

        if (!eligible)
        {
            throw ServiceException.Forbidden("You are not eligible to act on this approval.");
        }

        var blockedByLowerSequence = await _db.Approvals
            .AnyAsync(a => a.PurchaseOrderId == approval.PurchaseOrderId
                           && a.SequenceOrder < approval.SequenceOrder
                           && a.Status == ApprovalStatus.Pending, cancellationToken);

        if (blockedByLowerSequence)
        {
            throw ServiceException.Conflict("This approval cannot be acted on yet: an earlier-sequence approval is still pending.");
        }

        return userId;
    }

    private async Task<bool> IsFullyApprovedAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        // "Fully approved" is a derived check, not a stored flag (docs/04).
        return !await _db.Approvals
            .AnyAsync(a => a.PurchaseOrderId == purchaseOrderId && a.Status != ApprovalStatus.Approved, cancellationToken);
    }

    /// <summary>
    /// Runs when every required Approval for a PO is Approved: flips the PO to Approved and,
    /// for a bid-based PO, copies the awarded SupplierBidItems into PurchaseOrderLineItems
    /// (docs/03/04). Direct-entry lines already exist; aggregates are recomputed either way.
    /// </summary>
    private async Task CompletePurchaseOrderApprovalAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders.FirstAsync(p => p.Id == purchaseOrderId, cancellationToken);

        po.Status = PurchaseOrderStatus.Approved;
        await _db.SaveChangesAsync(cancellationToken);

        var isBidBased = po.AwardedSupplierBidId is not null;

        if (po.AwardedSupplierBidId is int awardedBidId)
        {
            var bidItems = await _db.SupplierBidItems.AsNoTracking()
                .Where(bi => bi.SupplierBidId == awardedBidId)
                .OrderBy(bi => bi.Id)
                .ToListAsync(cancellationToken);

            foreach (var bidItem in bidItems)
            {
                var lineItem = new PurchaseOrderLineItem
                {
                    PurchaseOrderId = purchaseOrderId,
                    SourceSupplierBidItemId = bidItem.Id,
                    Description = bidItem.Description,
                    Quantity = bidItem.Quantity,
                    UnitCost = bidItem.UnitCost,
                    CurrencyCode = bidItem.CurrencyCode,
                    DiscountPercentage = bidItem.DiscountPercentage,
                    TaxPercentage = bidItem.TaxPercentage,
                };

                // Recompute rather than copy the bid's computed fields directly — same inputs,
                // server-side math stays the single source of truth (BidItemMath).
                BidItemMath.Apply(lineItem);
                _db.PurchaseOrderLineItems.Add(lineItem);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        await PurchaseOrderTotalsRecompute.RecomputeAsync(_db, po, isBidBased, cancellationToken);
    }

    // ----- Helpers -----

    private async Task<HashSet<int>> GetCurrentUserRoleIdsAsync(int userId, CancellationToken cancellationToken)
    {
        var roleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        return roleIds.ToHashSet();
    }

    private async Task<Approval> LoadTrackedApprovalAsync(int approvalId, CancellationToken cancellationToken)
    {
        return await _db.Approvals.FirstOrDefaultAsync(a => a.Id == approvalId, cancellationToken)
            ?? throw ServiceException.NotFound($"Approval {approvalId} was not found.");
    }

    private void ApplyConcurrencyToken(Approval approval, string? rowVersion)
    {
        if (ConcurrencyToken.TryDecode(rowVersion, out var original))
        {
            _db.Entry(approval).Property<uint>("xmin").OriginalValue = original;
        }
    }

    private async Task SaveWithConcurrencyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ServiceException.Conflict("The approval was modified by someone else. Reload and try again.");
        }
    }

    private async Task<ApprovalDto> ToApprovalDtoAsync(int approvalId, CancellationToken cancellationToken)
    {
        var approval = await _db.Approvals
            .Include(a => a.RequiredRole)
            .Include(a => a.RequiredUser)
            .Include(a => a.ApprovedByUser)
            .FirstAsync(a => a.Id == approvalId, cancellationToken);

        return new ApprovalDto
        {
            Id = approval.Id,
            PurchaseOrderId = approval.PurchaseOrderId,
            RequiredRoleId = approval.RequiredRoleId,
            RequiredRoleName = approval.RequiredRole?.Name,
            RequiredUserId = approval.RequiredUserId,
            RequiredUserName = approval.RequiredUser?.FullName,
            SequenceOrder = approval.SequenceOrder,
            Status = approval.Status,
            ApprovedByUserId = approval.ApprovedByUserId,
            ApprovedByUserName = approval.ApprovedByUser?.FullName,
            ApprovedAtUtc = approval.ApprovedAtUtc,
            Comment = approval.Comment,
            RowVersion = ConcurrencyToken.Encode(_db.Entry(approval).Property<uint>("xmin").CurrentValue),
        };
    }
}
