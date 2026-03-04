using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Domain.Events;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed class OnInsuranceStatusChangedNotify(InsuranceDbContext db, IMediator m)
    : IDomainEventHandler<InsuranceStatusChangedEvent>
{
    private static readonly NotificationChannel[] EmailAndInApp = [NotificationChannel.Email, NotificationChannel.InApp];

    public async Task Handle(InsuranceStatusChangedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var policy = await db.PolicyRecords.AsNoTracking()
            .FirstOrDefaultAsync(p => p.DealId == e.DealId, ct).ConfigureAwait(false);
        if (policy is null) return;

        var (title, body) = e.NewState switch
        {
            InsuranceState.Active => ("Insurance Active", "Your insurance policy is now active for this deal."),
            InsuranceState.NotActive => ("Insurance Inactive", "Your insurance policy is no longer active. Please contact support."),
            InsuranceState.Unknown => ("Insurance Status Unknown", "We are unable to verify your insurance status. We will keep checking."),
            _ => ("Insurance Update", $"Your insurance status changed to {e.NewState}.")
        };

        await m.Send(new NotifyUserCommand(
            policy.TenantUserId, "insurance_status_changed",
            title, body,
            new() { ["dealId"] = e.DealId.ToString(), ["oldState"] = e.OldState.ToString(), ["newState"] = e.NewState.ToString() },
            EmailAndInApp, e.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}
