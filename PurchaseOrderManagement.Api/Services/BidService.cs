using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.SupplierBids;
using PurchaseOrderManagement.Api.Entities;
using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Supplier bids are a standalone library (plan section A): a bid can exist on its own
/// (PurchaseOrderId null) and later be attached to a Draft PO. Bid items may source lines from
/// any of the supplier's quotations (not just "the" quotation for this bid), each carrying its
/// own Currency — totals are a per-currency vector, never converted/combined.
/// </summary>
public class BidService : IBidService
{
    private readonly AppDbContext _db;

    public BidService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SupplierBidSummaryDto>> ListAsync(int? supplierId, int? purchaseOrderId, bool? unattachedOnly, CancellationToken cancellationToken)
    {
        var query = _db.SupplierBids.AsNoTracking().AsQueryable();

        if (supplierId is int sid)
        {
            query = query.Where(sb => sb.SupplierId == sid);
        }

        if (purchaseOrderId is int poId)
        {
            query = query.Where(sb => sb.PurchaseOrderId == poId);
        }

        if (unattachedOnly == true)
        {
            query = query.Where(sb => sb.PurchaseOrderId == null);
        }

        return await ProjectSummariesAsync(query, cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierBidSummaryDto>> ListForPurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        await EnsurePurchaseOrderExistsAsync(purchaseOrderId, cancellationToken);

        var query = _db.SupplierBids.AsNoTracking().Where(sb => sb.PurchaseOrderId == purchaseOrderId);

        return await ProjectSummariesAsync(query, cancellationToken);
    }

    private async Task<IReadOnlyList<SupplierBidSummaryDto>> ProjectSummariesAsync(IQueryable<SupplierBid> query, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var bids = await query
            .OrderBy(sb => sb.Supplier.SupplierName).ThenBy(sb => sb.Id)
            .Select(sb => new
            {
                sb.Id,
                sb.PurchaseOrderId,
                sb.SupplierId,
                SupplierName = sb.Supplier.SupplierName,
                sb.Notes,
                ItemCount = sb.SupplierBidItems.Count,
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
                // QuotationCount = distinct quotations referenced by this bid's items via their source line.
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

    public async Task<SupplierBidDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        // NOTE: tracked (not AsNoTracking) on purpose — ToDto/ToItemDto read the xmin
        // concurrency token via _db.Entry(...).Property("xmin").CurrentValue, which is only
        // populated for tracked entities. Under AsNoTracking it reads back as 0, which made
        // every subsequent bid-item update fail with a 409 (token never matched).
        var bid = await _db.SupplierBids
            .Include(sb => sb.Supplier)
            .Include(sb => sb.SupplierBidItems)
                .ThenInclude(i => i.SourceQuotationLineItem)
                    .ThenInclude(l => l.Quotation)
            .FirstOrDefaultAsync(sb => sb.Id == id, cancellationToken);

        return bid is null ? null : ToDto(bid);
    }

    public async Task<SupplierBidDto> CreateAsync(int? purchaseOrderId, CreateSupplierBidRequest request, CancellationToken cancellationToken)
    {
        if (purchaseOrderId is int poId)
        {
            await EnsurePurchaseOrderExistsAsync(poId, cancellationToken);
        }

        var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (!supplierExists)
        {
            throw ServiceException.Validation($"Supplier {request.SupplierId} was not found.");
        }

        if (purchaseOrderId is int existingPoId)
        {
            // One bid per supplier per PO when attached.
            var duplicate = await _db.SupplierBids
                .AnyAsync(sb => sb.PurchaseOrderId == existingPoId && sb.SupplierId == request.SupplierId, cancellationToken);
            if (duplicate)
            {
                throw ServiceException.Conflict("This supplier already has a bid on this purchase order.");
            }
        }

        var bid = new SupplierBid
        {
            PurchaseOrderId = purchaseOrderId,
            SupplierId = request.SupplierId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        _db.SupplierBids.Add(bid);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(bid.Id, cancellationToken))!;
    }

    public async Task<SupplierBidDto> AttachToPurchaseOrderAsync(int supplierBidId, int purchaseOrderId, CancellationToken cancellationToken)
    {
        var bid = await _db.SupplierBids.FirstOrDefaultAsync(sb => sb.Id == supplierBidId, cancellationToken)
            ?? throw ServiceException.NotFound($"Supplier bid {supplierBidId} was not found.");

        if (bid.PurchaseOrderId is not null)
        {
            throw ServiceException.Conflict($"Supplier bid {supplierBidId} is already attached to purchase order {bid.PurchaseOrderId}.");
        }

        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken)
            ?? throw ServiceException.Validation($"Purchase order {purchaseOrderId} was not found.");

        if (po.Status != PurchaseOrderStatus.Draft)
        {
            throw ServiceException.Conflict($"Cannot attach a bid to purchase order {purchaseOrderId}: composition is frozen once it leaves Draft (current status: {po.Status}).");
        }

        var duplicate = await _db.SupplierBids
            .AnyAsync(sb => sb.PurchaseOrderId == purchaseOrderId && sb.SupplierId == bid.SupplierId, cancellationToken);
        if (duplicate)
        {
            throw ServiceException.Conflict("This supplier already has a bid attached to this purchase order.");
        }

        bid.PurchaseOrderId = purchaseOrderId;
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(bid.Id, cancellationToken))!;
    }

    public async Task<SupplierBidItemDto> AddItemAsync(int supplierBidId, CreateSupplierBidItemRequest request, CancellationToken cancellationToken)
    {
        var bid = await _db.SupplierBids.FirstOrDefaultAsync(sb => sb.Id == supplierBidId, cancellationToken)
            ?? throw ServiceException.NotFound($"Supplier bid {supplierBidId} was not found.");

        var sourceLine = await EnsureQuotationLineBelongsToSupplierAsync(request.SourceQuotationLineItemId, bid.SupplierId, cancellationToken);

        // Default Currency from the source quotation when not explicitly overridden.
        var currencyCode = string.IsNullOrWhiteSpace(request.Currency)
            ? sourceLine.Quotation.CurrencyCode
            : await CurrencyValidation.NormalizeAndValidateAsync(_db, request.Currency, cancellationToken);

        var item = new SupplierBidItem
        {
            SupplierBidId = supplierBidId,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            CurrencyCode = currencyCode,
            DiscountPercentage = request.DiscountPercentage,
            TaxPercentage = request.TaxPercentage,
            SourceQuotationLineItemId = request.SourceQuotationLineItemId,
        };

        BidItemMath.Apply(item);

        _db.SupplierBidItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return ToItemDto(item);
    }

    public async Task<SupplierBidItemDto> UpdateItemAsync(int supplierBidId, int itemId, UpdateSupplierBidItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _db.SupplierBidItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.SupplierBidId == supplierBidId, cancellationToken)
            ?? throw ServiceException.NotFound($"Bid item {itemId} was not found for supplier bid {supplierBidId}.");

        if (ConcurrencyToken.TryDecode(request.RowVersion, out var original))
        {
            _db.Entry(item).Property<uint>("xmin").OriginalValue = original;
        }

        var currencyCode = await CurrencyValidation.NormalizeAndValidateAsync(_db, request.Currency, cancellationToken);

        item.Description = request.Description.Trim();
        item.Quantity = request.Quantity;
        item.UnitCost = request.UnitCost;
        item.CurrencyCode = currencyCode;
        item.DiscountPercentage = request.DiscountPercentage;
        item.TaxPercentage = request.TaxPercentage;

        BidItemMath.Apply(item);

        await SaveWithConcurrencyAsync(cancellationToken);

        return ToItemDto(item);
    }

    public async Task RemoveItemAsync(int supplierBidId, int itemId, CancellationToken cancellationToken)
    {
        var item = await _db.SupplierBidItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.SupplierBidId == supplierBidId, cancellationToken)
            ?? throw ServiceException.NotFound($"Bid item {itemId} was not found for supplier bid {supplierBidId}.");

        // Remove() is converted to a soft delete by AppDbContext (docs/05).
        _db.SupplierBidItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SupplierBidDto> SeedItemsFromQuotationAsync(int supplierBidId, SeedBidItemsFromQuotationRequest request, CancellationToken cancellationToken)
    {
        var bid = await _db.SupplierBids.FirstOrDefaultAsync(sb => sb.Id == supplierBidId, cancellationToken)
            ?? throw ServiceException.NotFound($"Supplier bid {supplierBidId} was not found.");

        var quotation = await _db.Quotations.AsNoTracking()
            .Include(q => q.QuotationLineItems)
            .FirstOrDefaultAsync(q => q.Id == request.QuotationId, cancellationToken)
            ?? throw ServiceException.Validation($"Quotation {request.QuotationId} was not found.");

        if (quotation.SupplierId != bid.SupplierId)
        {
            throw ServiceException.Validation($"Quotation {request.QuotationId} does not belong to the same supplier as supplier bid {supplierBidId}.");
        }

        if (quotation.QuotationLineItems.Count == 0)
        {
            throw ServiceException.Validation("The selected quotation has no line items to copy.");
        }

        foreach (var line in quotation.QuotationLineItems.OrderBy(li => li.Id))
        {
            // Copy qty/desc/unitcost/currency; discount/tax default to none and are then editable (docs/02).
            var item = new SupplierBidItem
            {
                SupplierBidId = supplierBidId,
                SourceQuotationLineItemId = line.Id,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                CurrencyCode = quotation.CurrencyCode,
                DiscountPercentage = null,
                TaxPercentage = null,
            };

            BidItemMath.Apply(item);
            _db.SupplierBidItems.Add(item);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(supplierBidId, cancellationToken))!;
    }

    private async Task SaveWithConcurrencyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ServiceException.Conflict("The bid item was modified by someone else. Reload and try again.");
        }
    }

    private SupplierBidDto ToDto(SupplierBid bid)
    {
        var rowVersion = ConcurrencyToken.Encode(_db.Entry(bid).Property<uint>("xmin").CurrentValue);

        var items = bid.SupplierBidItems
            .OrderBy(i => i.Id)
            .Select(ToItemDto)
            .ToList();

        var totals = items
            .GroupBy(i => i.Currency)
            .Select(g => new CurrencyTotalDto
            {
                Currency = g.Key,
                Subtotal = g.Sum(i => i.LineSubtotal),
                TaxAmount = g.Sum(i => i.TaxAmount),
                TotalAmount = g.Sum(i => i.LineTotal),
            })
            .OrderBy(t => t.Currency)
            .ToList();

        return new SupplierBidDto
        {
            Id = bid.Id,
            PurchaseOrderId = bid.PurchaseOrderId,
            SupplierId = bid.SupplierId,
            SupplierName = bid.Supplier.SupplierName,
            Notes = bid.Notes,
            Totals = totals,
            ItemCount = items.Count,
            Items = items,
            RowVersion = rowVersion,
        };
    }

    private SupplierBidItemDto ToItemDto(SupplierBidItem item) => new()
    {
        Id = item.Id,
        SupplierBidId = item.SupplierBidId,
        SourceQuotationLineItemId = item.SourceQuotationLineItemId,
        SourceQuotationId = item.SourceQuotationLineItem.QuotationId,
        SourceQuotationReference = item.SourceQuotationLineItem.Quotation.QuoteReference,
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

    private async Task EnsurePurchaseOrderExistsAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        var exists = await _db.PurchaseOrders.AnyAsync(po => po.Id == purchaseOrderId, cancellationToken);
        if (!exists)
        {
            throw ServiceException.NotFound($"Purchase order {purchaseOrderId} was not found.");
        }
    }

    private async Task<QuotationLineItem> EnsureQuotationLineBelongsToSupplierAsync(int quotationLineItemId, int supplierId, CancellationToken cancellationToken)
    {
        var line = await _db.QuotationLineItems
            .Include(li => li.Quotation)
            .FirstOrDefaultAsync(li => li.Id == quotationLineItemId, cancellationToken)
            ?? throw ServiceException.Validation($"Quotation line item {quotationLineItemId} was not found.");

        if (line.Quotation.SupplierId != supplierId)
        {
            throw ServiceException.Validation($"Quotation line item {quotationLineItemId} does not belong to the bid's supplier.");
        }

        return line;
    }
}
