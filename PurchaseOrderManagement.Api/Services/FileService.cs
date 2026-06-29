using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Files;
using PurchaseOrderManagement.Api.Entities;
using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Services;

public class FileService : IFileService
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly IFileUrlResolver _fileUrlResolver;

    public FileService(AppDbContext db, IFileStorage fileStorage, IFileUrlResolver fileUrlResolver)
    {
        _db = db;
        _fileStorage = fileStorage;
        _fileUrlResolver = fileUrlResolver;
    }

    public async Task<FileDto> UploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw ServiceException.Validation("A non-empty file must be provided.");
        }

        await using var stream = file.OpenReadStream();
        var storedRelativePath = await _fileStorage.SaveAsync(stream, file.FileName, cancellationToken);

        var storedFile = new StoredFile
        {
            SourceType = FileSourceType.Path,
            Source = storedRelativePath,
            OriginalFileName = file.FileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType,
            FileSizeBytes = file.Length,
        };

        _db.Files.Add(storedFile);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(storedFile);
    }

    public async Task<FileContentResultDto?> GetContentAsync(int id, CancellationToken cancellationToken)
    {
        var storedFile = await _db.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (storedFile is null)
        {
            return null;
        }

        if (storedFile.SourceType == FileSourceType.Url)
        {
            return new FileContentResultDto
            {
                SourceType = FileSourceType.Url,
                RedirectUrl = storedFile.Source,
            };
        }

        var content = _fileStorage.OpenRead(storedFile.Source);

        return new FileContentResultDto
        {
            SourceType = FileSourceType.Path,
            Content = content,
            ContentType = storedFile.ContentType,
            OriginalFileName = storedFile.OriginalFileName,
        };
    }

    private FileDto ToDto(StoredFile file) => new()
    {
        Id = file.Id,
        Url = _fileUrlResolver.Resolve(file),
        OriginalFileName = file.OriginalFileName,
        ContentType = file.ContentType,
        FileSizeBytes = file.FileSizeBytes,
    };
}
