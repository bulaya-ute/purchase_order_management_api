namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Encodes/decodes the PostgreSQL <c>xmin</c> concurrency token (a <see cref="uint"/>) as an
/// opaque base64 string for round-tripping through DTOs (docs/05-CROSS-CUTTING-CONVENTIONS.md).
/// </summary>
public static class ConcurrencyToken
{
    public static string Encode(uint xmin) => Convert.ToBase64String(BitConverter.GetBytes(xmin));

    public static bool TryDecode(string? token, out uint xmin)
    {
        xmin = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(token);
            if (bytes.Length != sizeof(uint))
            {
                return false;
            }

            xmin = BitConverter.ToUInt32(bytes, 0);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
