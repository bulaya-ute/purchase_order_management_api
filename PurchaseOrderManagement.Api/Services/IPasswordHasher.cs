namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// PBKDF2 password hashing, following the pattern carried over from the old WebForms
/// Login.aspx.cs (the one part of the old system worth keeping) — see
/// docs/01-IDENTITY-AND-ROLES.md and docs/05-CROSS-CUTTING-CONVENTIONS.md.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Derives a new random salt and PBKDF2 hash for the given plaintext password.</summary>
    (byte[] Hash, byte[] Salt) Create(string password);

    /// <summary>Verifies a plaintext password against a previously stored hash/salt using a constant-time compare.</summary>
    bool Verify(string password, byte[] hash, byte[] salt);
}
