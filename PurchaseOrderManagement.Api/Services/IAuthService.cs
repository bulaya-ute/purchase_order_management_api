using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

public interface IAuthService
{
    /// <summary>
    /// Verifies the email/password against an active user. Returns null on any failure
    /// (unknown email, wrong password, inactive user) — callers should respond with a single
    /// generic 401 message regardless of which case occurred, to avoid leaking which part failed.
    /// </summary>
    Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);
}
