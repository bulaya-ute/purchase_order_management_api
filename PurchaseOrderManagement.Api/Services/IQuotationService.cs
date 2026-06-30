using PurchaseOrderManagement.Api.Dtos.Quotations;

namespace PurchaseOrderManagement.Api.Services;

public interface IQuotationService
{
    /// <summary>Standalone quotation library list with optional filters (plan section A).</summary>
    Task<IReadOnlyList<QuotationSummaryDto>> ListAsync(int? supplierId, bool? isExpired, bool? isUsed, CancellationToken cancellationToken);
    Task<QuotationDto?> GetAsync(int quotationId, CancellationToken cancellationToken);
    Task<QuotationDto> CreateAsync(CreateQuotationRequest request, CancellationToken cancellationToken);
}
