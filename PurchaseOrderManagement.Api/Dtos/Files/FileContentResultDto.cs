using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Dtos.Files;

/// <summary>
/// What the GET /api/files/{id} endpoint needs to either stream bytes (SourceType.Path) or
/// redirect (SourceType.Url) — kept out of the controller so the service stays the source of truth.
/// </summary>
public class FileContentResultDto
{
    public FileSourceType SourceType { get; set; }

    /// <summary>Set when SourceType == Path: an open stream ready to read from the start.</summary>
    public Stream? Content { get; set; }

    /// <summary>Set when SourceType == Url: the external URL to redirect to.</summary>
    public string? RedirectUrl { get; set; }

    public string? ContentType { get; set; }
    public string? OriginalFileName { get; set; }
}
