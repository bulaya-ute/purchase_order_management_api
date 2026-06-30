using PurchaseOrderManagement.Api.Dtos.SupplierBids;

namespace PurchaseOrderManagement.Api.Services;

public interface IBidService
{
    /// <summary>Standalone bids library list with optional filters (plan section A).</summary>
    Task<IReadOnlyList<SupplierBidSummaryDto>> ListAsync(int? supplierId, int? purchaseOrderId, bool? unattachedOnly, CancellationToken cancellationToken);
    Task<IReadOnlyList<SupplierBidSummaryDto>> ListForPurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken);
    Task<SupplierBidDto?> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Creates a bid. purchaseOrderId is null for a standalone/library bid.</summary>
    Task<SupplierBidDto> CreateAsync(int? purchaseOrderId, CreateSupplierBidRequest request, CancellationToken cancellationToken);

    /// <summary>Attaches an existing standalone bid to a Draft PO.</summary>
    Task<SupplierBidDto> AttachToPurchaseOrderAsync(int supplierBidId, int purchaseOrderId, CancellationToken cancellationToken);

    Task<SupplierBidItemDto> AddItemAsync(int supplierBidId, CreateSupplierBidItemRequest request, CancellationToken cancellationToken);
    Task<SupplierBidItemDto> UpdateItemAsync(int supplierBidId, int itemId, UpdateSupplierBidItemRequest request, CancellationToken cancellationToken);
    Task RemoveItemAsync(int supplierBidId, int itemId, CancellationToken cancellationToken);

    Task<SupplierBidDto> SeedItemsFromQuotationAsync(int supplierBidId, SeedBidItemsFromQuotationRequest request, CancellationToken cancellationToken);
}
