using System.Security.Cryptography;
using System.Text;

namespace Lagedra.TruthSurface.Infrastructure.Crypto;

/// <summary>
/// Builds a Merkle tree from a list of line-item hashes.
/// Each leaf is the SHA-256 of a single line item's canonical JSON.
/// The root hash proves inclusion of every line item in a single proof.
/// </summary>
public static class MerkleTreeBuilder
{
    public static string ComputeRoot(IReadOnlyList<string> leafHashes)
    {
        ArgumentNullException.ThrowIfNull(leafHashes);

        if (leafHashes.Count == 0)
        {
            throw new ArgumentException("At least one leaf hash is required.", nameof(leafHashes));
        }

        var currentLevel = leafHashes.Select(NormalizeHash).ToList();

        while (currentLevel.Count > 1)
        {
            var nextLevel = new List<string>();

            for (var i = 0; i < currentLevel.Count; i += 2)
            {
                var left = currentLevel[i];
                var right = i + 1 < currentLevel.Count ? currentLevel[i + 1] : left;
                nextLevel.Add(HashPair(left, right));
            }

            currentLevel = nextLevel;
        }

        return currentLevel[0];
    }

    private static string NormalizeHash(string hash) =>
        hash.ToUpperInvariant();

    private static string HashPair(string left, string right)
    {
        var combined = string.CompareOrdinal(left, right) <= 0
            ? left + right
            : right + left;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }
}
