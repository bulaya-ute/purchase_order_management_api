namespace PurchaseOrderManagement.Api.Dtos.Files;

/// <summary>
/// A file-bearing DTO returns only the resolved URL — never the raw stored path
/// (docs/02-SUPPLIERS-AND-PROCUREMENT.md, docs/05-CROSS-CUTTING-CONVENTIONS.md).
/// </summary>
public class FileDto
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
}
