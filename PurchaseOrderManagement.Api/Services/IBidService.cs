using PurchaseOrderManagement.Api.Dtos.SupplierBids;

namespace PurchaseOrderManagement.Api.Services;

public interface IBidService
{
    Task<IReadOnlyList<SupplierBidSummaryDto>> ListForPurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken);
    Task<SupplierBidDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<SupplierBidDto> CreateAsync(int purchaseOrderId, CreateSupplierBidRequest request, CancellationToken cancellationToken);

    Task<SupplierBidItemDto> AddItemAsync(int supplierBidId, CreateSupplierBidItemRequest request, CancellationToken cancellationToken);
    Task<SupplierBidItemDto> UpdateItemAsync(int supplierBidId, int itemId, UpdateSupplierBidItemRequest request, CancellationToken cancellationToken);
    Task RemoveItemAsync(int supplierBidId, int itemId, CancellationToken cancellationToken);

    Task<SupplierBidDto> SeedItemsFromQuotationAsync(int supplierBidId, SeedBidItemsFromQuotationRequest request, CancellationToken cancellationToken);
}
