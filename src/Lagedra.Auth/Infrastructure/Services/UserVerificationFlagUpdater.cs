using Lagedra.Auth.Infrastructure.Persistence;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class UserVerificationFlagUpdater(AuthDbContext dbContext) : IUserVerificationFlagUpdater
{
    public async Task MarkGovernmentIdVerifiedAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            return;
        }

        user.IsGovernmentIdVerified = true;
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetHostKindAsync(
        Guid userId,
        HostKind hostKind,
        string? legalBusinessName,
        CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            return;
        }

        user.HostKind = hostKind;
        if (hostKind == HostKind.PropertyManagementCompany &&
            !string.IsNullOrWhiteSpace(legalBusinessName))
        {
            user.CompanyName = legalBusinessName.Trim();
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
