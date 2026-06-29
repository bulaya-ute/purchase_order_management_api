using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Resolves the fully-qualified URL for a <see cref="StoredFile"/> at the API layer, per request.
/// The fully-qualified URL is never persisted (docs/05-CROSS-CUTTING-CONVENTIONS.md, File storage
/// &amp; URLs section): if <see cref="FileSourceType.Url"/>, the stored Source is already a complete
/// external URL and is returned as-is; if <see cref="FileSourceType.Path"/>, the Source is a
/// server-relative path resolved by routing through the GET /api/files/{id} serving endpoint.
/// </summary>
public interface IFileUrlResolver
{
    string Resolve(StoredFile file);
}
