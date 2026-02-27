using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Services;

public sealed class HostPaymentDetailsProvider(
    IdentityDbContext dbContext,
    IEncryptionService encryptionService) : IHostPaymentDetailsProvider
{
    public async Task<HostPaymentDetailsDto?> GetDecryptedPaymentDetailsAsync(
        Guid hostUserId,
        CancellationToken ct = default)
    {
        var details = await dbContext.HostPaymentDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HostUserId == hostUserId, ct)
            .ConfigureAwait(false);

        if (details is null)
        {
            return null;
        }

        var decrypted = encryptionService.Decrypt(details.EncryptedPaymentInfo);
        return new HostPaymentDetailsDto(hostUserId, decrypted);
    }
}
