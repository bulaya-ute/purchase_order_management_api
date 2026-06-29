using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Files;
using PurchaseOrderManagement.Api.Dtos.Quotations;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

public class QuotationService : IQuotationService
{
    private readonly AppDbContext _db;
    private readonly IFileUrlResolver _fileUrlResolver;

    public QuotationService(AppDbContext db, IFileUrlResolver fileUrlResolver)
    {
        _db = db;
        _fileUrlResolver = fileUrlResolver;
    }

    public async Task<IReadOnlyList<QuotationSummaryDto>> ListForBidAsync(int supplierBidId, CancellationToken cancellationToken)
    {
        await EnsureBidExistsAsync(supplierBidId, cancellationToken);

        var quotations = await _db.Quotations.AsNoTracking()
            .Where(q => q.SupplierBidId == supplierBidId)
            .OrderByDescending(q => q.QuoteDate).ThenBy(q => q.Id)
            .Select(q => new
            {
                q.Id,
                q.SupplierBidId,
                q.FileId,
                File = q.File,
                q.QuoteReference,
                q.QuoteDate,
                q.ExpiresAtUtc,
                q.Notes,
                LineItemCount = q.QuotationLineItems.Count,
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        return quotations.Select(q => new QuotationSummaryDto
        {
            Id = q.Id,
            SupplierBidId = q.SupplierBidId,
            FileId = q.FileId,
            FileUrl = _fileUrlResolver.Resolve(q.File),
            OriginalFileName = q.File.OriginalFileName,
            QuoteReference = q.QuoteReference,
            QuoteDate = q.QuoteDate,
            ExpiresAtUtc = q.ExpiresAtUtc,
            IsExpired = q.ExpiresAtUtc.HasValue && q.ExpiresAtUtc.Value < now,
            Notes = q.Notes,
            LineItemCount = q.LineItemCount,
        }).ToList();
    }

    public async Task<QuotationDto?> GetAsync(int supplierBidId, int quotationId, CancellationToken cancellationToken)
    {
        var quotation = await _db.Quotations.AsNoTracking()
            .Include(q => q.File)
            .Include(q => q.QuotationLineItems)
            .Where(q => q.SupplierBidId == supplierBidId && q.Id == quotationId)
            .FirstOrDefaultAsync(cancellationToken);

        return quotation is null ? null : ToDto(quotation);
    }

    public async Task<QuotationDto> CreateAsync(int supplierBidId, CreateQuotationRequest request, CancellationToken cancellationToken)
    {
        await EnsureBidExistsAsync(supplierBidId, cancellationToken);

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

        var quotation = new Quotation
        {
            SupplierBidId = supplierBidId,
            FileId = request.FileId,
            QuoteReference = string.IsNullOrWhiteSpace(request.QuoteReference) ? null : request.QuoteReference.Trim(),
            QuoteDate = request.QuoteDate,
            ExpiresAtUtc = request.ExpiresAtUtc,
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

        return (await GetAsync(supplierBidId, quotation.Id, cancellationToken))!;
    }

    private async Task EnsureBidExistsAsync(int supplierBidId, CancellationToken cancellationToken)
    {
        var exists = await _db.SupplierBids.AnyAsync(sb => sb.Id == supplierBidId, cancellationToken);
        if (!exists)
        {
            throw ServiceException.NotFound($"Supplier bid {supplierBidId} was not found.");
        }
    }

    private QuotationDto ToDto(Quotation quotation)
    {
        var now = DateTime.UtcNow;
        return new QuotationDto
        {
            Id = quotation.Id,
            SupplierBidId = quotation.SupplierBidId,
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
            Notes = quotation.Notes,
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
