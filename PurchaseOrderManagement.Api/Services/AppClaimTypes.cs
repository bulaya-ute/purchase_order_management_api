namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Custom claim type used in the auth cookie alongside the standard
/// <see cref="System.Security.Claims.ClaimTypes"/> (NameIdentifier for UserId, Role per held role).
/// </summary>
public static class AppClaimTypes
{
    public const string CompanyId = "company_id";
}
