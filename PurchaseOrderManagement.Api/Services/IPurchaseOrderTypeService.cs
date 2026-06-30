using PurchaseOrderManagement.Api.Dtos.PurchaseOrderTypes;

namespace PurchaseOrderManagement.Api.Services;

public interface IPurchaseOrderTypeService
{
    Task<IReadOnlyList<PurchaseOrderTypeDto>> ListAsync(CancellationToken cancellationToken);
    Task<PurchaseOrderTypeDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<PurchaseOrderTypeDto> CreateAsync(CreatePurchaseOrderTypeRequest request, CancellationToken cancellationToken);
    Task<PurchaseOrderTypeDto> UpdateAsync(int id, UpdatePurchaseOrderTypeRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
