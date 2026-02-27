namespace Lagedra.SharedKernel.Integration;

public interface IHostPaymentDetailsProvider
{
    Task<HostPaymentDetailsDto?> GetDecryptedPaymentDetailsAsync(
        Guid hostUserId,
        CancellationToken ct = default);
}
