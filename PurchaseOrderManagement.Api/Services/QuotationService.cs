using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Files;
using PurchaseOrderManagement.Api.Dtos.Quotations;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Quotations are a standalone library (plan section A): obtained from suppliers before any
/// bid/PO exists, kept for audit/future reference, and optionally "used" later by sourcing one of
/// their line items into a SupplierBidItem (any bid for the same supplier, not just "the" bid).
/// </summary>
public class QuotationService : IQuotationService
{
    private readonly AppDbContext _db;
    private readonly IFileUrlResolver _fileUrlResolver;

    public QuotationService(AppDbContext db, IFileUrlResolver fileUrlResolver)
    {
        _db = db;
        _fileUrlResolver = fileUrlResolver;
    }

    public async Task<IReadOnlyList<QuotationSummaryDto>> ListAsync(int? supplierId, bool? isExpired, bool? isUsed, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var query = _db.Quotations.AsNoTracking().AsQueryable();

        if (supplierId is int sid)
        {
            query = query.Where(q => q.SupplierId == sid);
        }

        if (isExpired is bool expired)
        {
            query = expired
                ? query.Where(q => q.ExpiresAtUtc != null && q.ExpiresAtUtc.Value < now)
                : query.Where(q => q.ExpiresAtUtc == null || q.ExpiresAtUtc.Value >= now);
        }

        var quotations = await query
            .OrderByDescending(q => q.QuoteDate).ThenBy(q => q.Id)
            .Select(q => new
            {
                q.Id,
                q.SupplierId,
                SupplierName = q.Supplier.SupplierName,
                q.FileId,
                File = q.File,
                q.QuoteReference,
                q.QuoteDate,
                q.ExpiresAtUtc,
                q.CurrencyCode,
                q.Notes,
                LineItemCount = q.QuotationLineItems.Count,
                IsUsed = q.QuotationLineItems.Any(li => li.SupplierBidItems.Any()),
            })
            .ToListAsync(cancellationToken);

        var projected = quotations.Select(q => new QuotationSummaryDto
        {
            Id = q.Id,
            SupplierId = q.SupplierId,
            SupplierName = q.SupplierName,
            FileId = q.FileId,
            FileUrl = _fileUrlResolver.Resolve(q.File),
            OriginalFileName = q.File.OriginalFileName,
            QuoteReference = q.QuoteReference,
            QuoteDate = q.QuoteDate,
            ExpiresAtUtc = q.ExpiresAtUtc,
            IsExpired = q.ExpiresAtUtc.HasValue && q.ExpiresAtUtc.Value < now,
            Currency = q.CurrencyCode,
            Notes = q.Notes,
            LineItemCount = q.LineItemCount,
            IsUsed = q.IsUsed,
        });

        if (isUsed is bool used)
        {
            projected = projected.Where(q => q.IsUsed == used);
        }

        return projected.ToList();
    }

    public async Task<QuotationDto?> GetAsync(int quotationId, CancellationToken cancellationToken)
    {
        var quotation = await _db.Quotations.AsNoTracking()
            .Include(q => q.Supplier)
            .Include(q => q.File)
            .Include(q => q.QuotationLineItems).ThenInclude(li => li.SupplierBidItems)
            .FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);

        return quotation is null ? null : ToDto(quotation);
    }

    public async Task<QuotationDto> CreateAsync(CreateQuotationRequest request, CancellationToken cancellationToken)
    {
        var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (!supplierExists)
        {
            throw ServiceException.Validation($"Supplier {request.SupplierId} was not found.");
        }

        // FileId is mandatory — reject creation without a valid uploaded file (docs/02).
        var fileExists = await _db.Files.AnyAsync(f => f.Id == request.FileId, cancellationToken);
        if (!fileExists)
        {
            throw ServiceException.Validation($"File {request.FileId} was not found. A quotation requires a valid uploaded file.");
        }

        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            throw ServiceException.Validation("A quotation requires at least one line item.");
        }

        var currencyCode = await CurrencyValidation.NormalizeAndValidateAsync(_db, request.Currency, cancellationToken);

        var quotation = new Quotation
        {
            SupplierId = request.SupplierId,
            FileId = request.FileId,
            QuoteReference = string.IsNullOrWhiteSpace(request.QuoteReference) ? null : request.QuoteReference.Trim(),
            QuoteDate = request.QuoteDate,
            ExpiresAtUtc = request.ExpiresAtUtc,
            CurrencyCode = currencyCode,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        foreach (var line in request.LineItems)
        {
            quotation.QuotationLineItems.Add(new QuotationLineItem
            {
                Description = line.Description.Trim(),
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
            });
        }

        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(quotation.Id, cancellationToken))!;
    }

    private QuotationDto ToDto(Quotation quotation)
    {
        var now = DateTime.UtcNow;
        return new QuotationDto
        {
            Id = quotation.Id,
            SupplierId = quotation.SupplierId,
            SupplierName = quotation.Supplier.SupplierName,
            File = new FileDto
            {
                Id = quotation.File.Id,
                Url = _fileUrlResolver.Resolve(quotation.File),
                OriginalFileName = quotation.File.OriginalFileName,
                ContentType = quotation.File.ContentType,
                FileSizeBytes = quotation.File.FileSizeBytes,
            },
            QuoteReference = quotation.QuoteReference,
            QuoteDate = quotation.QuoteDate,
            ExpiresAtUtc = quotation.ExpiresAtUtc,
            IsExpired = quotation.ExpiresAtUtc.HasValue && quotation.ExpiresAtUtc.Value < now,
            Currency = quotation.CurrencyCode,
            Notes = quotation.Notes,
            IsUsed = quotation.QuotationLineItems.Any(li => li.SupplierBidItems.Any()),
            LineItems = quotation.QuotationLineItems
                .OrderBy(li => li.Id)
                .Select(li => new QuotationLineItemDto
                {
                    Id = li.Id,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitCost = li.UnitCost,
                })
                .ToList(),
        };
    }
}
