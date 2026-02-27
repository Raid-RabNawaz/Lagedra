using Lagedra.Modules.Arbitration.Domain.Events;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.EventHandlers;

internal static class Channels
{
    internal static readonly NotificationChannel[] EmailAndInApp = [NotificationChannel.Email, NotificationChannel.InApp];
    internal static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];
}

public sealed class OnCaseFiledNotify(ArbitrationDbContext db, IMediator m)
    : IDomainEventHandler<CaseFiledEvent>
{
    public async Task Handle(CaseFiledEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var arbitrationCase = await db.ArbitrationCases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == e.CaseId, ct).ConfigureAwait(false);
        if (arbitrationCase is null)
        {
            return;
        }

        await m.Send(new NotifyUserCommand(
            arbitrationCase.FiledByUserId, "arbitration_case_filed",
            "Arbitration Case Filed",
            "Your arbitration case has been filed and is being reviewed.",
            new() { ["caseId"] = e.CaseId.ToString(), ["dealId"] = e.DealId.ToString() },
            Channels.EmailAndInApp, e.CaseId, "ArbitrationCase"), ct).ConfigureAwait(false);
    }
}

public sealed class OnDecisionIssuedNotify(ArbitrationDbContext db, IMediator m)
    : IDomainEventHandler<DecisionIssuedEvent>
{
    public async Task Handle(DecisionIssuedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var arbitrationCase = await db.ArbitrationCases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == e.CaseId, ct).ConfigureAwait(false);
        if (arbitrationCase is null)
        {
            return;
        }

        await m.Send(new NotifyUserCommand(
            arbitrationCase.FiledByUserId, "arbitration_decision",
            "Arbitration Decision Issued",
            "A decision has been issued for your arbitration case. Please review the outcome.",
            new() { ["caseId"] = e.CaseId.ToString(), ["dealId"] = e.DealId.ToString(), ["tier"] = e.Tier.ToString() },
            Channels.EmailAndInApp, e.CaseId, "ArbitrationCase"), ct).ConfigureAwait(false);
    }
}

public sealed class OnEvidenceCompleteNotify(ArbitrationDbContext db, IMediator m)
    : IDomainEventHandler<EvidenceCompleteEvent>
{
    public async Task Handle(EvidenceCompleteEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var arbitrationCase = await db.ArbitrationCases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == e.CaseId, ct).ConfigureAwait(false);
        if (arbitrationCase is null)
        {
            return;
        }

        await m.Send(new NotifyUserCommand(
            arbitrationCase.FiledByUserId, "evidence_complete",
            "Evidence Collection Complete",
            $"All evidence has been submitted. A decision is expected by {e.DecisionDueAt:MMM dd, yyyy}.",
            new() { ["caseId"] = e.CaseId.ToString(), ["decisionDueAt"] = e.DecisionDueAt.ToString("o") },
            Channels.InAppOnly, e.CaseId, "ArbitrationCase"), ct).ConfigureAwait(false);
    }
}
