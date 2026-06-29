namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Low-level disk persistence for uploaded files, under the configurable `FileStorage:Path` root
/// (docs/05-CROSS-CUTTING-CONVENTIONS.md, File storage &amp; URLs section). Generates a unique
/// on-disk filename so the original filename (kept separately on the StoredFile row) never
/// collides or needs sanitizing for filesystem use.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists the stream to disk and returns the relative path stored as Files.Source.</summary>
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken);

    /// <summary>Opens the file at the given relative path (as stored in Files.Source) for reading.</summary>
    Stream OpenRead(string relativePath);
}
