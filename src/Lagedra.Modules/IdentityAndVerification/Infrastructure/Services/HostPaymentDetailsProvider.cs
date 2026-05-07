using System.Data.Common;
using System.Security.Cryptography;
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
        var details = default(Lagedra.Modules.IdentityAndVerification.Domain.Entities.HostPaymentDetails);
        try
        {
            details = await dbContext.HostPaymentDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.HostUserId == hostUserId, ct)
                .ConfigureAwait(false);
        }
        catch (DbException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        if (details is null)
        {
            return null;
        }

        try
        {
            var decrypted = encryptionService.Decrypt(details.EncryptedPaymentInfo);
            return new HostPaymentDetailsDto(hostUserId, decrypted);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
