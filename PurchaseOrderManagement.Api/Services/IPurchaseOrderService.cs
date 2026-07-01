using PurchaseOrderManagement.Api.Dtos.Approvals;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.PurchaseOrders;

namespace PurchaseOrderManagement.Api.Services;

public interface IPurchaseOrderService
{
    Task<PagedResult<PurchaseOrderSummaryDto>> ListAsync(PurchaseOrderListQuery query, CancellationToken cancellationToken);
    Task<PurchaseOrderDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderRequest request, CancellationToken cancellationToken);

    // ----- Composition: Supplier Bid attachment (Draft only, lock on primary) -----
    Task<PurchaseOrderDto> AttachSupplierBidAsync(int purchaseOrderId, int supplierBidId, bool isPrimary, CancellationToken cancellationToken);
    Task DetachSupplierBidAsync(int purchaseOrderId, int supplierBidId, CancellationToken cancellationToken);
    Task SetPrimarySupplierBidAsync(int purchaseOrderId, int supplierBidId, CancellationToken cancellationToken);

    // ----- Composition (Draft only) -----
    Task<PurchaseOrderLineItemDto> AddLineItemAsync(int purchaseOrderId, CreatePurchaseOrderLineItemRequest request, CancellationToken cancellationToken);
    Task<PurchaseOrderLineItemDto> UpdateLineItemAsync(int purchaseOrderId, int lineItemId, UpdatePurchaseOrderLineItemRequest request, CancellationToken cancellationToken);
    Task RemoveLineItemAsync(int purchaseOrderId, int lineItemId, CancellationToken cancellationToken);

    Task<PurchaseOrderDto> SelectAwardedBidAsync(int purchaseOrderId, SelectAwardedBidRequest request, CancellationToken cancellationToken);

    Task<ApprovalDto> AddApprovalDefinitionAsync(int purchaseOrderId, CreateApprovalDefinitionRequest request, CancellationToken cancellationToken);
    Task RemoveApprovalDefinitionAsync(int purchaseOrderId, int approvalId, CancellationToken cancellationToken);

    // ----- Lifecycle -----
    Task<PurchaseOrderDto> SubmitAsync(int purchaseOrderId, CancellationToken cancellationToken);
    Task<PurchaseOrderDto> PayAsync(int purchaseOrderId, CancellationToken cancellationToken);
    Task<PurchaseOrderDto> DeliverAsync(int purchaseOrderId, CancellationToken cancellationToken);
    Task<PurchaseOrderDto> CancelAsync(int purchaseOrderId, CancellationToken cancellationToken);

    // ----- Approvals on a PO -----
    Task<IReadOnlyList<ApprovalDto>> ListApprovalsAsync(int purchaseOrderId, CancellationToken cancellationToken);
}
