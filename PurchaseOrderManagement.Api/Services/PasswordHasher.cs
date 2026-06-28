using System.Security.Cryptography;

namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// PBKDF2 (Rfc2898DeriveBytes) password hashing: SHA256, 100,000 iterations, 16-byte random
/// salt, 32-byte derived subkey. See docs/01-IDENTITY-AND-ROLES.md and
/// docs/05-CROSS-CUTTING-CONVENTIONS.md (Authentication section).
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int SubkeySizeBytes = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public (byte[] Hash, byte[] Salt) Create(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, SubkeySizeBytes);
        return (hash, salt);
    }

    public bool Verify(string password, byte[] hash, byte[] salt)
    {
        var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, SubkeySizeBytes);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }
}
