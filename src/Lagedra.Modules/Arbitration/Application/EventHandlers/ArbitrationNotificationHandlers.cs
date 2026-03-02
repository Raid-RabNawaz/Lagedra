using Lagedra.Modules.Arbitration.Domain.Events;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

public sealed class OnCaseClosedNotify(ArbitrationDbContext db, IMediator m)
    : IDomainEventHandler<CaseClosedEvent>
{
    public async Task Handle(CaseClosedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var arbitrationCase = await db.ArbitrationCases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == e.CaseId, ct).ConfigureAwait(false);
        if (arbitrationCase is null) return;

        await m.Send(new NotifyUserCommand(
            arbitrationCase.FiledByUserId, "arbitration_case_closed",
            "Arbitration Case Closed",
            "Your arbitration case has been formally closed.",
            new() { ["caseId"] = e.CaseId.ToString(), ["dealId"] = e.DealId.ToString() },
            Channels.EmailAndInApp, e.CaseId, "ArbitrationCase"), ct).ConfigureAwait(false);
    }
}

public sealed class OnCaseAppealedNotify(ArbitrationDbContext db, IMediator m)
    : IDomainEventHandler<CaseAppealedEvent>
{
    public async Task Handle(CaseAppealedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        var arbitrationCase = await db.ArbitrationCases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == e.CaseId, ct).ConfigureAwait(false);
        if (arbitrationCase is null) return;

        await m.Send(new NotifyUserCommand(
            arbitrationCase.FiledByUserId, "arbitration_case_appealed",
            "Arbitration Case Appealed",
            $"An appeal has been filed for your arbitration case. Reason: {e.Reason}",
            new() { ["caseId"] = e.CaseId.ToString(), ["dealId"] = e.DealId.ToString(), ["reason"] = e.Reason },
            Channels.EmailAndInApp, e.CaseId, "ArbitrationCase"), ct).ConfigureAwait(false);
    }
}

public sealed partial class OnBacklogEscalationHandler(ILogger<OnBacklogEscalationHandler> logger)
    : IDomainEventHandler<ArbitrationBacklogEscalationEvent>
{
    public Task Handle(ArbitrationBacklogEscalationEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        LogBacklogEscalation(logger, e.OverdueCaseCount, e.CheckedAt);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "ARBITRATION BACKLOG ESCALATION: {OverdueCaseCount} overdue cases detected at {CheckedAt}")]
    private static partial void LogBacklogEscalation(ILogger logger, int overdueCaseCount, DateTime checkedAt);
}
