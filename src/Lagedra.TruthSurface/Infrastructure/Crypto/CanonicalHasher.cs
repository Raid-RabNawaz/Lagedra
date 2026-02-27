using System.Security.Cryptography;
using System.Text;

namespace Lagedra.TruthSurface.Infrastructure.Crypto;

/// <summary>
/// Computes a deterministic SHA-256 hash from canonical JSON content.
/// The input must already be in canonical form (sorted keys, no whitespace variance)
/// before being passed here — this class does NOT re-serialize.
/// </summary>
public static class CanonicalHasher
{
    public static string ComputeHash(string canonicalJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalJson);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }
}
