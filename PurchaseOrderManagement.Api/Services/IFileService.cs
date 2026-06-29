using Microsoft.AspNetCore.Http;
using PurchaseOrderManagement.Api.Dtos.Files;

namespace PurchaseOrderManagement.Api.Services;

public interface IFileService
{
    Task<FileDto> UploadAsync(IFormFile file, CancellationToken cancellationToken);
    Task<FileContentResultDto?> GetContentAsync(int id, CancellationToken cancellationToken);
}
