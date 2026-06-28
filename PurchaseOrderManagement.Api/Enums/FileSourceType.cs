namespace PurchaseOrderManagement.Api.Enums;

/// <summary>
/// Whether a <see cref="Entities.StoredFile"/>'s Source is a server-relative path or an
/// already-complete external URL. Resolution of the final URL happens at the API layer.
/// </summary>
public enum FileSourceType
{
    Path,
    Url
}
