using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.SupplierBids;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

public class BidService : IBidService
{
    private readonly AppDbContext _db;

    public BidService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SupplierBidSummaryDto>> ListForPurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        await EnsurePurchaseOrderExistsAsync(purchaseOrderId, cancellationToken);

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
                BidTotal = sb.SupplierBidItems.Sum(i => (decimal?)i.LineTotal) ?? 0m,
                ItemCount = sb.SupplierBidItems.Count,
                QuotationCount = sb.Quotations.Count,
                ExpiryDates = sb.Quotations
                    .Where(q => q.ExpiresAtUtc != null)
                    .Select(q => q.ExpiresAtUtc!.Value)
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
            BidTotal = b.BidTotal,
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
            .FirstOrDefaultAsync(sb => sb.Id == id, cancellationToken);

        return bid is null ? null : ToDto(bid);
    }

    public async Task<SupplierBidDto> CreateAsync(int purchaseOrderId, CreateSupplierBidRequest request, CancellationToken cancellationToken)
    {
        await EnsurePurchaseOrderExistsAsync(purchaseOrderId, cancellationToken);

        var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (!supplierExists)
        {
            throw ServiceException.Validation($"Supplier {request.SupplierId} was not found.");
        }

        // SupplierBid is keyed by PurchaseOrderId + SupplierId — one bid per supplier per PO.
        var duplicate = await _db.SupplierBids
            .AnyAsync(sb => sb.PurchaseOrderId == purchaseOrderId && sb.SupplierId == request.SupplierId, cancellationToken);
        if (duplicate)
        {
            throw ServiceException.Conflict("This supplier already has a bid on this purchase order.");
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

    public async Task<SupplierBidItemDto> AddItemAsync(int supplierBidId, CreateSupplierBidItemRequest request, CancellationToken cancellationToken)
    {
        await EnsureBidExistsAsync(supplierBidId, cancellationToken);

        if (request.SourceQuotationLineItemId is int sourceId)
        {
            await EnsureQuotationLineBelongsToBidAsync(sourceId, supplierBidId, cancellationToken);
        }

        var item = new SupplierBidItem
        {
            SupplierBidId = supplierBidId,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
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

        item.Description = request.Description.Trim();
        item.Quantity = request.Quantity;
        item.UnitCost = request.UnitCost;
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
        await EnsureBidExistsAsync(supplierBidId, cancellationToken);

        var quotation = await _db.Quotations.AsNoTracking()
            .Include(q => q.QuotationLineItems)
            .FirstOrDefaultAsync(q => q.Id == request.QuotationId, cancellationToken)
            ?? throw ServiceException.Validation($"Quotation {request.QuotationId} was not found.");

        if (quotation.SupplierBidId != supplierBidId)
        {
            throw ServiceException.Validation($"Quotation {request.QuotationId} does not belong to supplier bid {supplierBidId}.");
        }

        if (quotation.QuotationLineItems.Count == 0)
        {
            throw ServiceException.Validation("The selected quotation has no line items to copy.");
        }

        foreach (var line in quotation.QuotationLineItems.OrderBy(li => li.Id))
        {
            // Copy qty/desc/unitcost; discount/tax default to none and are then editable (docs/02).
            var item = new SupplierBidItem
            {
                SupplierBidId = supplierBidId,
                SourceQuotationLineItemId = line.Id,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
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

        return new SupplierBidDto
        {
            Id = bid.Id,
            PurchaseOrderId = bid.PurchaseOrderId,
            SupplierId = bid.SupplierId,
            SupplierName = bid.Supplier.SupplierName,
            Notes = bid.Notes,
            BidTotal = items.Sum(i => i.LineTotal),
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
        Description = item.Description,
        Quantity = item.Quantity,
        UnitCost = item.UnitCost,
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

    private async Task EnsureBidExistsAsync(int supplierBidId, CancellationToken cancellationToken)
    {
        var exists = await _db.SupplierBids.AnyAsync(sb => sb.Id == supplierBidId, cancellationToken);
        if (!exists)
        {
            throw ServiceException.NotFound($"Supplier bid {supplierBidId} was not found.");
        }
    }

    private async Task EnsureQuotationLineBelongsToBidAsync(int quotationLineItemId, int supplierBidId, CancellationToken cancellationToken)
    {
        var belongs = await _db.QuotationLineItems
            .AnyAsync(li => li.Id == quotationLineItemId && li.Quotation.SupplierBidId == supplierBidId, cancellationToken);
        if (!belongs)
        {
            throw ServiceException.Validation($"Quotation line item {quotationLineItemId} does not belong to supplier bid {supplierBidId}.");
        }
    }
}
