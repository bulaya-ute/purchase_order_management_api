using Microsoft.Extensions.Configuration;
using PurchaseOrderManagement.Api.Entities;
using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Services;

/// <inheritdoc cref="IFileUrlResolver" />
public class FileUrlResolver : IFileUrlResolver
{
    private readonly string _baseUrl;

    public FileUrlResolver(IConfiguration configuration)
    {
        // FileStorage:BaseUrl — e.g. "https://api.example.com" — combined with the dedicated
        // file-serving endpoint for SourceType.Path files (docs/05-CROSS-CUTTING-CONVENTIONS.md).
        _baseUrl = (configuration["FileStorage:BaseUrl"] ?? string.Empty).TrimEnd('/');
    }

    public string Resolve(StoredFile file)
    {
        if (file.SourceType == FileSourceType.Url)
        {
            return file.Source;
        }

        // SourceType.Path: route through the GET /api/files/{id} serving endpoint rather than
        // exposing the raw on-disk path — keeps storage location/domain changes config-only.
        return $"{_baseUrl}/api/files/{file.Id}";
    }
}
