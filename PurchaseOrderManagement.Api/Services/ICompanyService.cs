using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.Companies;

namespace PurchaseOrderManagement.Api.Services;

public interface ICompanyService
{
    Task<PagedResult<CompanyDto>> ListAsync(PagedQuery query, CancellationToken cancellationToken);
    Task<CompanyDto?> GetAsync(int id, CancellationToken cancellationToken);
    Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken);
    Task<CompanyDto> UpdateAsync(int id, UpdateCompanyRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
