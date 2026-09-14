using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.EventHandlers;

public sealed class OnIdentityVerificationFailedRaiseFraudFlagHandler(IdentityDbContext dbContext)
    : IDomainEventHandler<IdentityVerificationFailedEvent>
{
    private static readonly TimeSpan ReviewSla = TimeSpan.FromHours(24);

    public async Task Handle(IdentityVerificationFailedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var alreadyOpen = await dbContext.FraudFlags
            .AnyAsync(f => f.UserId == e.UserId && f.ResolvedAt == null, ct)
            .ConfigureAwait(false);

        if (alreadyOpen)
        {
            return;
        }

        dbContext.FraudFlags.Add(FraudFlag.Create(
            e.UserId,
            e.Reason,
            "identity-verification",
            ReviewSla));

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
