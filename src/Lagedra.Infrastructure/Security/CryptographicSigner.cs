using System.Security.Cryptography;
using System.Text;
using Lagedra.SharedKernel.Security;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Infrastructure.Security;

public sealed class CryptographicSigner : ICryptographicSigner
{
    private readonly byte[] _key;

    public CryptographicSigner(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var secret = configuration["Signing:Secret"]
            ?? throw new InvalidOperationException("Signing:Secret is not configured.");
        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string Sign(byte[] data)
    {
        var hash = HMACSHA256.HashData(_key, data);
        return Convert.ToBase64String(hash);
    }

    public bool Verify(byte[] data, string signature)
    {
        var expected = Sign(data);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }
}
