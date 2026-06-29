using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.Suppliers;

namespace PurchaseOrderManagement.Api.Services;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> ListAsync(SupplierListQuery query, CancellationToken cancellationToken);
    Task<SupplierDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken);
    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
