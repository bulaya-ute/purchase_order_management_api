using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Common;

/// <summary>
/// Common pagination query parameters. Page defaults to 1, PageSize to 20 and is capped at 100
/// so a client can't request an unbounded result set.
/// </summary>
public class PagedQuery
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    [Range(1, int.MaxValue)]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }
}
