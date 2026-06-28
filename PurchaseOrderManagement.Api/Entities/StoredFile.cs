using PurchaseOrderManagement.Api.Enums;

namespace PurchaseOrderManagement.Api.Entities;

/// <summary>
/// A generic file reference used anywhere a document needs to be attached (currently: quotations).
/// Named StoredFile (mapped to table "Files") because "File" clashes with System.IO.File.
/// See docs/02-SUPPLIERS-AND-PROCUREMENT.md.
/// </summary>
public class StoredFile : BaseEntity
{
    /// <summary>Whether Source is a server-relative path or an already-complete external URL.</summary>
    public FileSourceType SourceType { get; set; }

    /// <summary>The path or URL itself — never a pre-built, fully-qualified URL.</summary>
    public string Source { get; set; } = null!;

    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }

    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
}
