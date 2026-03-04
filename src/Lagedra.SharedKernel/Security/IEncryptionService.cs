namespace Lagedra.SharedKernel.Security;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertextBase64);
}
