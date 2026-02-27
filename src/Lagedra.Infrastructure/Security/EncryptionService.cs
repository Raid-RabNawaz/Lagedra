using System.Security.Cryptography;
using System.Text;
using Lagedra.SharedKernel.Security;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Infrastructure.Security;

public sealed class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var keyString = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is not configured.");

        _key = Convert.FromBase64String(keyString);

        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Encryption:Key must be a 256-bit (32-byte) Base64-encoded key.");
        }
    }

    public string Encrypt(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextData = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertextData, tag);

        var result = new byte[nonce.Length + tag.Length + ciphertextData.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        ciphertextData.CopyTo(result, nonce.Length + tag.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertextBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ciphertextBase64);

        var combined = Convert.FromBase64String(ciphertextBase64);

        var nonce = combined.AsSpan(0, 12);
        var tag = combined.AsSpan(12, 16);
        var ciphertext = combined.AsSpan(28);

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
