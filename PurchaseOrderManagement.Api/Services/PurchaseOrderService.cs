using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Approvals;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.PurchaseOrders;
using PurchaseOrderManagement.Api.Dtos.SupplierBids;
using PurchaseOrderManagement.Api.Entities;
using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PurchaseOrderService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ----- List / read -----

    public async Task<PagedResult<PurchaseOrderSummaryDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = _db.PurchaseOrders.AsNoTracking();

        // ASSUMPTION (docs/08 [DECIDE], flagged): results default-scope to the current user's
        // CompanyId unless an explicit CompanyId filter is supplied. There is no documented
        // notion yet of a "see all companies" privilege, so this is the conservative default.
        var companyFilter = query.CompanyId ?? _currentUser.CompanyId;
        if (companyFilter is int companyId)
        {
            baseQuery = baseQuery.Where(po => po.CompanyId == companyId);
        }

        if (query.Status is PurchaseOrderStatus status)
        {
            baseQuery = baseQuery.Where(po => po.Status == status);
        }

        var ordered = baseQuery.OrderByDescending(po => po.Id);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(po => new PurchaseOrderSummaryDto
            {
                Id = po.Id,
                PONumber = po.PONumber,
                CompanyId = po.CompanyId,
                CompanyName = po.Company.Name,
                TargetCompanyId = po.TargetCompanyId,
                TargetCompanyName = po.TargetCompany == null ? null : po.TargetCompany.Name,
                IssuerUserId = po.IssuerUserId,
                IssuerUserName = po.IssuerUser.FullName,
                Currency = po.CurrencyCode,
                Status = po.Status,
                TotalAmount = po.TotalAmount,
                Notes = po.Notes,
                PaidAtUtc = po.PaidAtUtc,
                DeliveredAtUtc = po.DeliveredAtUtc,
                CreatedAtUtc = po.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrderSummaryDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<PurchaseOrderDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        // Tracked (not AsNoTracking) on purpose — ToDto reads the xmin concurrency token via
        // _db.Entry(...).Property("xmin").CurrentValue, which only populates for tracked
        // entities (see BidService for the same note).
        var po = await _db.PurchaseOrders
            .Include(p => p.Company)
            .Include(p => p.TargetCompany)
            .Include(p => p.IssuerUser)
            .Include(p => p.PurchaseOrderType)
            .Include(p => p.PurchaseOrderLineItems)
            .Include(p => p.CurrencyTotals)
            .Include(p => p.Approvals).ThenInclude(a => a.RequiredRole)
            .Include(p => p.Approvals).ThenInclude(a => a.RequiredUser)
            .Include(p => p.Approvals).ThenInclude(a => a.ApprovedByUser)
            .Include(p => p.AttachedSupplierBids)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (po is null)
        {
            return null;
        }

        var bidSummaries = await GetBidSummariesAsync(po.Id, cancellationToken);

        return ToDto(po, bidSummaries);
    }

    // ----- Create / update header -----

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var companyExists = await _db.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (!companyExists)
        {
            throw ServiceException.Validation($"Company {request.CompanyId} was not found.");
        }

        if (request.TargetCompanyId is int targetCompanyId)
        {
            var targetExists = await _db.Companies.AnyAsync(c => c.Id == targetCompanyId, cancellationToken);
            if (!targetExists)
            {
                throw ServiceException.Validation($"Target company {targetCompanyId} was not found.");
            }
        }

        var issuerUserId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("An authenticated user is required to create a purchase order.");

        // Currency is optional — defaults to ZMW server-side when omitted (plan section D).
        // Always validated against the active Currency table (so a missing/deactivated seed
        // surfaces a clear error here, not a confusing FK violation at SaveChanges).
        var currencyCode = await CurrencyValidation.NormalizeAndValidateAsync(
            _db, request.Currency ?? CurrencyValidation.DefaultCurrencyCode, cancellationToken);

        PurchaseOrderType? poType = null;
        if (request.PurchaseOrderTypeId is int typeId)
        {
            poType = await _db.PurchaseOrderTypes
                .Include(t => t.ApprovalSteps)
                .Include(t => t.AllowedCreatorRoles)
                .FirstOrDefaultAsync(t => t.Id == typeId, cancellationToken)
                ?? throw ServiceException.Validation($"Purchase order type {typeId} was not found.");

            if (!poType.IsActive)
            {
                throw ServiceException.Validation($"Purchase order type {typeId} is not active.");
            }

            await EnsureCreatorAllowedForTypeAsync(poType, issuerUserId, cancellationToken);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var po = new PurchaseOrder
        {
            CompanyId = request.CompanyId,
            TargetCompanyId = request.TargetCompanyId,
            IssuerUserId = issuerUserId,
            CurrencyCode = currencyCode,
            PurchaseOrderTypeId = poType?.Id,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = PurchaseOrderStatus.Draft,
            // PONumber requires the Id, which only exists after the first insert (docs/03).
            PONumber = string.Empty,
        };

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(cancellationToken);

        po.PONumber = "PO-" + po.Id.ToString("D4");

        if (poType is not null)
        {
            // Auto-generate Approval rows directly from the type's steps — bypasses
            // AddApprovalDefinitionAsync entirely, and the chain is then immutable (plan section C).
            foreach (var step in poType.ApprovalSteps.OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id))
            {
                _db.Approvals.Add(new Approval
                {
                    PurchaseOrderId = po.Id,
                    RequiredRoleId = step.RequiredRoleId,
                    RequiredUserId = step.RequiredUserId,
                    SequenceOrder = step.SequenceOrder,
                    Status = ApprovalStatus.Pending,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return (await GetAsync(po.Id, cancellationToken))!;
    }

    public async Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(id, cancellationToken);

        EnsureDraft(po, "edit");

        ApplyConcurrencyToken(po, request.RowVersion);

        if (request.TargetCompanyId is int targetCompanyId)
        {
            var targetExists = await _db.Companies.AnyAsync(c => c.Id == targetCompanyId, cancellationToken);
            if (!targetExists)
            {
                throw ServiceException.Validation($"Target company {targetCompanyId} was not found.");
            }
        }

        po.CurrencyCode = await CurrencyValidation.NormalizeAndValidateAsync(_db, request.Currency, cancellationToken);
        po.TargetCompanyId = request.TargetCompanyId;
        po.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await SaveWithConcurrencyAsync(cancellationToken, "purchase order");

        return (await GetAsync(po.Id, cancellationToken))!;
    }

    // ----- Composition: direct-entry line items (Draft only) -----

    public async Task<PurchaseOrderLineItemDto> AddLineItemAsync(int purchaseOrderId, CreatePurchaseOrderLineItemRequest request, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        EnsureDraft(po, "add a line item to");

        var item = new PurchaseOrderLineItem
        {
            PurchaseOrderId = purchaseOrderId,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            // Direct-entry POs are single-currency: lines inherit the PO's own Currency (docs/03).
            CurrencyCode = po.CurrencyCode,
            DiscountPercentage = request.DiscountPercentage,
            TaxPercentage = request.TaxPercentage,
        };

        BidItemMath.Apply(item);

        _db.PurchaseOrderLineItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        await RecomputeAggregatesAsync(purchaseOrderId, cancellationToken);

        return await ToLineItemDtoAsync(item.Id, cancellationToken);
    }

    public async Task<PurchaseOrderLineItemDto> UpdateLineItemAsync(int purchaseOrderId, int lineItemId, UpdatePurchaseOrderLineItemRequest request, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        EnsureDraft(po, "edit a line item on");

        var item = await _db.PurchaseOrderLineItems
            .FirstOrDefaultAsync(li => li.Id == lineItemId && li.PurchaseOrderId == purchaseOrderId, cancellationToken)
            ?? throw ServiceException.NotFound($"Line item {lineItemId} was not found for purchase order {purchaseOrderId}.");

        if (ConcurrencyToken.TryDecode(request.RowVersion, out var original))
        {
            _db.Entry(item).Property<uint>("xmin").OriginalValue = original;
        }

        item.Description = request.Description.Trim();
        item.Quantity = request.Quantity;
        item.UnitCost = request.UnitCost;
        item.CurrencyCode = po.CurrencyCode;
        item.DiscountPercentage = request.DiscountPercentage;
        item.TaxPercentage = request.TaxPercentage;

        BidItemMath.Apply(item);

        await SaveWithConcurrencyAsync(cancellationToken, "line item");

        await RecomputeAggregatesAsync(purchaseOrderId, cancellationToken);

        return await ToLineItemDtoAsync(item.Id, cancellationToken);
    }

    public async Task RemoveLineItemAsync(int purchaseOrderId, int lineItemId, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        EnsureDraft(po, "remove a line item from");

        var item = await _db.PurchaseOrderLineItems
            .FirstOrDefaultAsync(li => li.Id == lineItemId && li.PurchaseOrderId == purchaseOrderId, cancellationToken)
            ?? throw ServiceException.NotFound($"Line item {lineItemId} was not found for purchase order {purchaseOrderId}.");

        // Remove() is converted to a soft delete by AppDbContext (docs/05).
        _db.PurchaseOrderLineItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);

        await RecomputeAggregatesAsync(purchaseOrderId, cancellationToken);
    }

    // ----- Composition: Supplier Bid attachment (Draft only, lock on primary) -----

    public async Task<PurchaseOrderDto> AttachSupplierBidAsync(int purchaseOrderId, int supplierBidId, bool isPrimary, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);

        var actingUserId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("An authenticated user is required.");

        if (po.IssuerUserId != actingUserId)
        {
            throw ServiceException.Forbidden("Only the PO creator may attach supplier bids.");
        }

        if (po.Status != PurchaseOrderStatus.Draft)
        {
            throw ServiceException.Validation("Supplier bids can only be attached while the PO is in Draft.");
        }

        var primaryAlreadySet = await _db.PurchaseOrderSupplierBids
            .AnyAsync(posb => posb.PurchaseOrderId == purchaseOrderId && posb.IsPrimary, cancellationToken);
        if (primaryAlreadySet)
        {
            throw ServiceException.Validation("Supplier bids are locked because a primary bid has already been set.");
        }

        var alreadyAttached = await _db.PurchaseOrderSupplierBids
            .AnyAsync(posb => posb.PurchaseOrderId == purchaseOrderId && posb.SupplierBidId == supplierBidId, cancellationToken);
        if (alreadyAttached)
        {
            throw ServiceException.Validation("This bid is already attached to the PO.");
        }

        var bidExists = await _db.SupplierBids.AnyAsync(sb => sb.Id == supplierBidId, cancellationToken);
        if (!bidExists)
        {
            throw ServiceException.NotFound($"Supplier bid {supplierBidId} was not found.");
        }

        _db.PurchaseOrderSupplierBids.Add(new PurchaseOrderSupplierBid
        {
            PurchaseOrderId = purchaseOrderId,
            SupplierBidId = supplierBidId,
            IsPrimary = isPrimary,
            AddedAtUtc = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(purchaseOrderId, cancellationToken))!;
    }

    public async Task DetachSupplierBidAsync(int purchaseOrderId, int supplierBidId, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);

        var actingUserId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("An authenticated user is required.");

        if (po.IssuerUserId != actingUserId)
        {
            throw ServiceException.Forbidden("Only the PO creator may detach supplier bids.");
        }

        if (po.Status != PurchaseOrderStatus.Draft)
        {
            throw ServiceException.Validation("Supplier bids can only be detached while the PO is in Draft.");
        }

        var primaryAlreadySet = await _db.PurchaseOrderSupplierBids
            .AnyAsync(posb => posb.PurchaseOrderId == purchaseOrderId && posb.IsPrimary, cancellationToken);
        if (primaryAlreadySet)
        {
            throw ServiceException.Validation("Supplier bids are locked because a primary bid has already been set.");
        }

        var junction = await _db.PurchaseOrderSupplierBids
            .FirstOrDefaultAsync(posb => posb.PurchaseOrderId == purchaseOrderId && posb.SupplierBidId == supplierBidId, cancellationToken)
            ?? throw ServiceException.NotFound("This bid is not attached to the PO.");

        _db.PurchaseOrderSupplierBids.Remove(junction);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPrimarySupplierBidAsync(int purchaseOrderId, int supplierBidId, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);

        var primaryAlreadySet = await _db.PurchaseOrderSupplierBids
            .AnyAsync(posb => posb.PurchaseOrderId == purchaseOrderId && posb.IsPrimary, cancellationToken);
        if (primaryAlreadySet)
        {
            throw ServiceException.Validation("A primary bid is already set and cannot be changed.");
        }

        // Permission check: PO creator (any status), OR any user who appears anywhere in the
        // approval chain — whether as a named user or via a role they hold.
        var actingUserId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("An authenticated user is required.");

        var isCreator = po.IssuerUserId == actingUserId;

        bool isAnyApproverInChain = !isCreator && await _db.Approvals
            .AnyAsync(a => a.PurchaseOrderId == purchaseOrderId &&
                (a.RequiredUserId == actingUserId ||
                 (a.RequiredRoleId != null &&
                  _db.UserRoles.Any(ur => ur.UserId == actingUserId && ur.RoleId == a.RequiredRoleId))),
                cancellationToken);

        if (!isCreator && !isAnyApproverInChain)
        {
            throw ServiceException.Forbidden("Only the PO creator or a member of the approval chain may award a bid.");
        }

        var junction = await _db.PurchaseOrderSupplierBids
            .FirstOrDefaultAsync(posb => posb.PurchaseOrderId == purchaseOrderId && posb.SupplierBidId == supplierBidId, cancellationToken)
            ?? throw ServiceException.NotFound("This bid is not attached to the PO.");

        junction.IsPrimary = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Composition: awarded bid selection (Draft only) -----

    public async Task<PurchaseOrderDto> SelectAwardedBidAsync(int purchaseOrderId, SelectAwardedBidRequest request, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        EnsureDraft(po, "select an awarded bid for");

        var bid = await _db.SupplierBids.AsNoTracking()
            .FirstOrDefaultAsync(sb => sb.Id == request.SupplierBidId, cancellationToken)
            ?? throw ServiceException.Validation($"Supplier bid {request.SupplierBidId} was not found.");

        if (bid.PurchaseOrderId != purchaseOrderId)
        {
            throw ServiceException.Validation($"Supplier bid {request.SupplierBidId} does not belong to purchase order {purchaseOrderId}.");
        }

        var actingUserId = _currentUser.UserId
            ?? throw ServiceException.Forbidden("An authenticated user is required to award a bid.");

        po.AwardedSupplierBidId = bid.Id;
        po.AwardedAtUtc = DateTime.UtcNow;
        po.AwardedByUserId = actingUserId;

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(purchaseOrderId, cancellationToken))!;
    }

    // ----- Composition: approval definitions (Draft only) -----

    public async Task<ApprovalDto> AddApprovalDefinitionAsync(int purchaseOrderId, CreateApprovalDefinitionRequest request, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        EnsureDraft(po, "define approvals for");
        EnsureNotTyped(po, "define approvals for");

        var hasRole = request.RequiredRoleId.HasValue;
        var hasUser = request.RequiredUserId.HasValue;
        if (hasRole == hasUser)
        {
            throw ServiceException.Validation("Exactly one of RequiredRoleId or RequiredUserId must be supplied.");
        }

        if (request.RequiredRoleId is int roleId)
        {
            var roleExists = await _db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
            if (!roleExists)
            {
                throw ServiceException.Validation($"Role {roleId} was not found.");
            }
        }

        if (request.RequiredUserId is int userId)
        {
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                throw ServiceException.Validation($"User {userId} was not found.");
            }
        }

        var approval = new Approval
        {
            PurchaseOrderId = purchaseOrderId,
            RequiredRoleId = request.RequiredRoleId,
            RequiredUserId = request.RequiredUserId,
            SequenceOrder = request.SequenceOrder,
            Status = ApprovalStatus.Pending,
        };

        _db.Approvals.Add(approval);
        await _db.SaveChangesAsync(cancellationToken);

        return await ToApprovalDtoAsync(approval.Id, cancellationToken);
    }

    public async Task RemoveApprovalDefinitionAsync(int purchaseOrderId, int approvalId, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        EnsureDraft(po, "remove an approval definition from");
        EnsureNotTyped(po, "remove an approval definition from");

        var approval = await _db.Approvals
            .FirstOrDefaultAsync(a => a.Id == approvalId && a.PurchaseOrderId == purchaseOrderId, cancellationToken)
            ?? throw ServiceException.NotFound($"Approval {approvalId} was not found for purchase order {purchaseOrderId}.");

        // Remove() is converted to a soft delete by AppDbContext (docs/05).
        _db.Approvals.Remove(approval);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Lifecycle -----

    public async Task<PurchaseOrderDto> SubmitAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        EnsureDraft(po, "submit");

        var hasBids = await _db.PurchaseOrderSupplierBids
            .AnyAsync(posb => posb.PurchaseOrderId == purchaseOrderId, cancellationToken);
        if (!hasBids)
        {
            throw ServiceException.Validation("At least one Supplier Bid must be attached before submitting a Purchase Order.");
        }

        var hasAwardedBid = po.AwardedSupplierBidId is not null;
        var hasLineItems = await _db.PurchaseOrderLineItems.AnyAsync(li => li.PurchaseOrderId == purchaseOrderId, cancellationToken);

        if (!hasAwardedBid && !hasLineItems)
        {
            throw ServiceException.Validation("The purchase order must have an awarded bid selected or at least one line item before it can be submitted.");
        }

        var hasApprovals = await _db.Approvals.AnyAsync(a => a.PurchaseOrderId == purchaseOrderId, cancellationToken);
        if (!hasApprovals)
        {
            throw ServiceException.Validation("The purchase order must have at least one approval defined before it can be submitted.");
        }

        // Approval rows already exist (created as Pending while Draft, per the definitions
        // composed there, or auto-generated from the PO's type); submit just flips the PO's
        // status to Open and freezes composition.
        po.Status = PurchaseOrderStatus.Open;

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(purchaseOrderId, cancellationToken))!;
    }

    public async Task<PurchaseOrderDto> PayAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);

        if (po.Status != PurchaseOrderStatus.Approved)
        {
            throw ServiceException.Validation("Only an Approved purchase order can be marked as paid.");
        }

        if (po.PaidAtUtc is null)
        {
            po.PaidAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return (await GetAsync(purchaseOrderId, cancellationToken))!;
    }

    public async Task<PurchaseOrderDto> DeliverAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);

        if (po.Status != PurchaseOrderStatus.Approved)
        {
            throw ServiceException.Validation("Only an Approved purchase order can be marked as delivered.");
        }

        if (po.DeliveredAtUtc is null)
        {
            po.DeliveredAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return (await GetAsync(purchaseOrderId, cancellationToken))!;
    }

    public async Task<PurchaseOrderDto> CancelAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var po = await GetTrackedPurchaseOrderAsync(purchaseOrderId, cancellationToken);

        var cancellable = po.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Open or PurchaseOrderStatus.Approved;
        if (!cancellable)
        {
            throw ServiceException.Validation($"A purchase order in status {po.Status} cannot be cancelled.");
        }

        if (po.PaidAtUtc is not null)
        {
            throw ServiceException.Validation("A purchase order that has already been paid cannot be cancelled.");
        }

        po.Status = PurchaseOrderStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(purchaseOrderId, cancellationToken))!;
    }

    // ----- Approvals list for a PO -----

    public async Task<IReadOnlyList<ApprovalDto>> ListApprovalsAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        await EnsurePurchaseOrderExistsAsync(purchaseOrderId, cancellationToken);

        var approvals = await _db.Approvals
            .Include(a => a.RequiredRole)
            .Include(a => a.RequiredUser)
            .Include(a => a.ApprovedByUser)
            .Where(a => a.PurchaseOrderId == purchaseOrderId)
            .OrderBy(a => a.SequenceOrder).ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return approvals.Select(ToApprovalDto).ToList();
    }

    // ----- Internal helpers -----

    internal async Task RecomputeAggregatesAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders.FirstAsync(p => p.Id == purchaseOrderId, cancellationToken);

        // Direct-entry edits (this method's only caller) are never bid-based.
        await PurchaseOrderTotalsRecompute.RecomputeAsync(_db, po, isBidBased: false, cancellationToken);
    }

    private async Task<PurchaseOrder> GetTrackedPurchaseOrderAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"Purchase order {id} was not found.");
    }

    private async Task EnsurePurchaseOrderExistsAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var exists = await _db.PurchaseOrders.AnyAsync(po => po.Id == purchaseOrderId, cancellationToken);
        if (!exists)
        {
            throw ServiceException.NotFound($"Purchase order {purchaseOrderId} was not found.");
        }
    }

    private static void EnsureDraft(PurchaseOrder po, string action)
    {
        if (po.Status != PurchaseOrderStatus.Draft)
        {
            throw ServiceException.Conflict($"Cannot {action} purchase order {po.Id}: composition is frozen once it leaves Draft (current status: {po.Status}).");
        }
    }

    /// <summary>
    /// Makes a typed PO's auto-generated approval chain immutable: the creator cannot add to or
    /// remove from it (plan section C — closes the "pick a friendly approver" loophole).
    /// </summary>
    private static void EnsureNotTyped(PurchaseOrder po, string action)
    {
        if (po.PurchaseOrderTypeId is not null)
        {
            throw ServiceException.Conflict($"Cannot {action} purchase order {po.Id}: its approval chain is fixed by its purchase order type and cannot be edited.");
        }
    }

    private async Task EnsureCreatorAllowedForTypeAsync(PurchaseOrderType poType, int userId, CancellationToken cancellationToken)
    {
        if (poType.AllowedCreatorRoles.Count == 0)
        {
            // No restriction configured for this type — anyone may create it.
            return;
        }

        var allowedRoleIds = poType.AllowedCreatorRoles.Select(r => r.RoleId).ToHashSet();

        var userRoleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (!userRoleIds.Any(allowedRoleIds.Contains))
        {
            throw ServiceException.Forbidden($"You do not hold a role permitted to create a purchase order of type '{poType.Name}'.");
        }
    }

    private void ApplyConcurrencyToken(PurchaseOrder po, string? rowVersion)
    {
        if (ConcurrencyToken.TryDecode(rowVersion, out var original))
        {
            _db.Entry(po).Property<uint>("xmin").OriginalValue = original;
        }
    }

    private async Task SaveWithConcurrencyAsync(CancellationToken cancellationToken, string what)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ServiceException.Conflict($"The {what} was modified by someone else. Reload and try again.");
        }
    }

    private async Task<IReadOnlyList<SupplierBidSummaryDto>> GetBidSummariesAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var bids = await _db.SupplierBids.AsNoTracking()
            .Where(sb => sb.PurchaseOrderId == purchaseOrderId)
            .OrderBy(sb => sb.Supplier.SupplierName).ThenBy(sb => sb.Id)
            .Select(sb => new
            {
                sb.Id,
                sb.PurchaseOrderId,
                sb.SupplierId,
                SupplierName = sb.Supplier.SupplierName,
                sb.Notes,
                Totals = sb.SupplierBidItems
                    .GroupBy(i => i.CurrencyCode)
                    .Select(g => new CurrencyTotalDto
                    {
                        Currency = g.Key,
                        Subtotal = g.Sum(i => i.LineSubtotal),
                        TaxAmount = g.Sum(i => i.TaxAmount),
                        TotalAmount = g.Sum(i => i.LineTotal),
                    })
                    .ToList(),
                ItemCount = sb.SupplierBidItems.Count,
                QuotationCount = sb.SupplierBidItems
                    .Select(i => i.SourceQuotationLineItem.QuotationId)
                    .Distinct()
                    .Count(),
                ExpiryDates = sb.SupplierBidItems
                    .Where(i => i.SourceQuotationLineItem.Quotation.ExpiresAtUtc != null)
                    .Select(i => i.SourceQuotationLineItem.Quotation.ExpiresAtUtc!.Value)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return bids.Select(b => new SupplierBidSummaryDto
        {
            Id = b.Id,
            PurchaseOrderId = b.PurchaseOrderId,
            SupplierId = b.SupplierId,
            SupplierName = b.SupplierName,
            Notes = b.Notes,
            Totals = b.Totals,
            ItemCount = b.ItemCount,
            QuotationCount = b.QuotationCount,
            HasExpiredQuotation = b.ExpiryDates.Any(d => d < now),
            EarliestQuotationExpiryUtc = b.ExpiryDates.Count == 0 ? null : b.ExpiryDates.Min(),
        }).ToList();
    }

    private async Task<PurchaseOrderLineItemDto> ToLineItemDtoAsync(int lineItemId, CancellationToken cancellationToken)
    {
        var item = await _db.PurchaseOrderLineItems.FirstAsync(li => li.Id == lineItemId, cancellationToken);
        return ToLineItemDto(item);
    }

    private PurchaseOrderLineItemDto ToLineItemDto(PurchaseOrderLineItem item) => new()
    {
        Id = item.Id,
        PurchaseOrderId = item.PurchaseOrderId,
        SourceSupplierBidItemId = item.SourceSupplierBidItemId,
        Description = item.Description,
        Quantity = item.Quantity,
        UnitCost = item.UnitCost,
        Currency = item.CurrencyCode,
        DiscountPercentage = item.DiscountPercentage,
        DiscountAmount = item.DiscountAmount,
        TaxPercentage = item.TaxPercentage,
        TaxAmount = item.TaxAmount,
        LineSubtotal = item.LineSubtotal,
        LineTotal = item.LineTotal,
        RowVersion = ConcurrencyToken.Encode(_db.Entry(item).Property<uint>("xmin").CurrentValue),
    };

    private async Task<ApprovalDto> ToApprovalDtoAsync(int approvalId, CancellationToken cancellationToken)
    {
        var approval = await _db.Approvals
            .Include(a => a.RequiredRole)
            .Include(a => a.RequiredUser)
            .Include(a => a.ApprovedByUser)
            .FirstAsync(a => a.Id == approvalId, cancellationToken);

        return ToApprovalDto(approval);
    }

    private ApprovalDto ToApprovalDto(Approval approval) => new()
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

    private PurchaseOrderDto ToDto(PurchaseOrder po, IReadOnlyList<SupplierBidSummaryDto> bidSummaries)
    {
        var lineItems = po.PurchaseOrderLineItems.OrderBy(li => li.Id).Select(ToLineItemDto).ToList();
        var approvals = po.Approvals.OrderBy(a => a.SequenceOrder).ThenBy(a => a.Id).Select(ToApprovalDto).ToList();

        var currencyTotals = po.CurrencyTotals
            .OrderBy(t => t.CurrencyCode)
            .Select(t => new CurrencyTotalDto
            {
                Currency = t.CurrencyCode,
                Subtotal = t.Subtotal,
                TaxAmount = t.TaxAmount,
                TotalAmount = t.TotalAmount,
            })
            .ToList();

        // A bid-based PO whose awarded bid's line items span more than one currency: the flat
        // Subtotal/TaxAmount/TotalAmount fields were zeroed by PurchaseOrderTotalsRecompute, and
        // the vector (more than one currency row) is authoritative instead.
        var hasMultiCurrencyTotals = currencyTotals.Count > 1;

        return new PurchaseOrderDto
        {
            Id = po.Id,
            PONumber = po.PONumber,
            CompanyId = po.CompanyId,
            CompanyName = po.Company.Name,
            TargetCompanyId = po.TargetCompanyId,
            TargetCompanyName = po.TargetCompany?.Name,
            IssuerUserId = po.IssuerUserId,
            IssuerUserName = po.IssuerUser.FullName,
            Currency = po.CurrencyCode,
            Status = po.Status,
            Notes = po.Notes,
            PurchaseOrderTypeId = po.PurchaseOrderTypeId,
            PurchaseOrderTypeName = po.PurchaseOrderType?.Name,
            AwardedSupplierBidId = po.AwardedSupplierBidId,
            AwardedAtUtc = po.AwardedAtUtc,
            AwardedByUserId = po.AwardedByUserId,
            PaidAtUtc = po.PaidAtUtc,
            DeliveredAtUtc = po.DeliveredAtUtc,
            Subtotal = po.Subtotal,
            TaxAmount = po.TaxAmount,
            TotalAmount = po.TotalAmount,
            HasMultiCurrencyTotals = hasMultiCurrencyTotals,
            Totals = currencyTotals,
            CreatedAtUtc = po.CreatedAtUtc,
            LineItems = lineItems,
            Approvals = approvals,
            SupplierBids = bidSummaries,
            AttachedSupplierBids = po.AttachedSupplierBids
                .OrderBy(posb => posb.AddedAtUtc)
                .Select(posb => new PurchaseOrderSupplierBidDto
                {
                    SupplierBidId = posb.SupplierBidId,
                    IsPrimary = posb.IsPrimary,
                    AddedAtUtc = posb.AddedAtUtc,
                })
                .ToList(),
            RowVersion = ConcurrencyToken.Encode(_db.Entry(po).Property<uint>("xmin").CurrentValue),
        };
    }
}
