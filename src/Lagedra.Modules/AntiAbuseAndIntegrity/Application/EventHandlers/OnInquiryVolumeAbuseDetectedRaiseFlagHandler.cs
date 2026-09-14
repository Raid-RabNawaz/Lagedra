using Lagedra.Modules.AntiAbuseAndIntegrity.Application.Services;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Aggregates;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.EventHandlers;

public sealed class OnInquiryVolumeAbuseDetectedRaiseFlagHandler(IntegrityDbContext dbContext)
    : IDomainEventHandler<InquiryVolumeAbuseDetectedEvent>
{
    public async Task Handle(InquiryVolumeAbuseDetectedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var alreadyFlagged = await dbContext.FraudFlags
            .AnyAsync(f => f.UserId == e.TenantUserId && f.FlagType == FraudFlagType.InquiryAbuse, ct)
            .ConfigureAwait(false);

        if (alreadyFlagged)
        {
            return;
        }

        await FraudFlagWriter
            .RaiseIfMissingAsync(
                dbContext,
                e.TenantUserId,
                FraudFlagType.InquiryAbuse,
                Severity.Medium,
                ct)
            .ConfigureAwait(false);

        dbContext.AbuseCases.Add(AbuseCase.Open(e.TenantUserId, AbuseType.InquiryAbuse));
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
