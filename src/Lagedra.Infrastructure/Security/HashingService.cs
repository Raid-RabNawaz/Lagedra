using System.Security.Cryptography;
using System.Text;
using Lagedra.SharedKernel.Security;

namespace Lagedra.Infrastructure.Security;

public sealed class HashingService : IHashingService
{
    public string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }

    public bool Verify(string value, string hash) =>
        Hash(value).Equals(hash, StringComparison.OrdinalIgnoreCase);
}
