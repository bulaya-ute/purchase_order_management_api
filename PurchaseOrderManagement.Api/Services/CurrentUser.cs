using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace PurchaseOrderManagement.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _principal = httpContextAccessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    public int? UserId
    {
        get
        {
            var value = _principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public int? CompanyId
    {
        get
        {
            var value = _principal?.FindFirstValue(AppClaimTypes.CompanyId);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        _principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();
}
