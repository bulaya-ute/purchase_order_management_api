namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Accessor for the acting user's identity, derived from the authenticated cookie-session
/// claims (docs/05-CROSS-CUTTING-CONVENTIONS.md, Authentication section). Null properties mean
/// there is no authenticated user for the current request (e.g. anonymous endpoints, or
/// system/seed operations running outside an HTTP request).
/// </summary>
public interface ICurrentUser
{
    int? UserId { get; }
    int? CompanyId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
}
