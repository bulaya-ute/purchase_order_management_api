using PurchaseOrderManagement.Api.Dtos.Quotations;

namespace PurchaseOrderManagement.Api.Services;

public interface IQuotationService
{
    Task<IReadOnlyList<QuotationSummaryDto>> ListForBidAsync(int supplierBidId, CancellationToken cancellationToken);
    Task<QuotationDto?> GetAsync(int supplierBidId, int quotationId, CancellationToken cancellationToken);
    Task<QuotationDto> CreateAsync(int supplierBidId, CreateQuotationRequest request, CancellationToken cancellationToken);
}
