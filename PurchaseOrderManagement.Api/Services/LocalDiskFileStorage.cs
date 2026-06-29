using Microsoft.Extensions.Configuration;

namespace PurchaseOrderManagement.Api.Services;

/// <inheritdoc cref="IFileStorage" />
public class LocalDiskFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalDiskFileStorage(IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:Path"];
        var relativeOrAbsolute = string.IsNullOrWhiteSpace(configuredPath) ? "App_Data/uploads" : configuredPath;

        _rootPath = Path.IsPathRooted(relativeOrAbsolute)
            ? relativeOrAbsolute
            : Path.Combine(AppContext.BaseDirectory, relativeOrAbsolute);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(originalFileName);
        var generatedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, generatedFileName);

        await using var destination = File.Create(fullPath);
        await content.CopyToAsync(destination, cancellationToken);

        // Stored as Files.Source — a server-relative identifier, never a fully-qualified path/URL.
        return generatedFileName;
    }

    public Stream OpenRead(string relativePath)
    {
        var fullPath = Path.Combine(_rootPath, relativePath);
        return File.OpenRead(fullPath);
    }
}
