namespace PurchaseOrderManagement.Api.Dtos.Common;

/// <summary>
/// Reusable paged-list envelope returned by list endpoints. Page is 1-based.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
