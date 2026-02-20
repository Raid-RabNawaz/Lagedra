namespace Lagedra.SharedKernel.Security;

public interface ICryptographicSigner
{
    string Sign(byte[] data);
    bool Verify(byte[] data, string signature);
}
