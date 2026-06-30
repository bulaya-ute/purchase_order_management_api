using PurchaseOrderManagement.Api.Dtos.Currencies;

namespace PurchaseOrderManagement.Api.Services;

public interface ICurrencyService
{
    Task<IReadOnlyList<CurrencyDto>> ListAsync(bool? isActive, CancellationToken cancellationToken);
    Task<CurrencyDto?> GetAsync(string code, CancellationToken cancellationToken);
    Task<CurrencyDto> CreateAsync(CreateCurrencyRequest request, CancellationToken cancellationToken);
    Task<CurrencyDto> UpdateAsync(string code, UpdateCurrencyRequest request, CancellationToken cancellationToken);
}
