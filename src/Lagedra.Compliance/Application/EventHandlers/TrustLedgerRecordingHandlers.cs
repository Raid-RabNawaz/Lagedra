using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.EventHandlers;

/// <summary>
/// Subscribes to every cross-module event that raises or lowers a user's
/// trust level and appends the matching trust ledger entry, so the ledger is
/// a complete record of trust-relevant activity. Deal-scoped signals
/// (deal completed, reviews, payment defaults, insurance lapses) flow through
/// the compliance-signal pipeline instead — see <see cref="CrossModuleSignalHandlers"/>.
/// </summary>
internal static class TrustLedgerRecorder
{
    /// <summary>
    /// Appends an entry unless an identical (user, type, reference) entry
    /// already exists — events can be redelivered at-least-once.
    /// </summary>
    internal static async Task RecordOnceAsync(
        ComplianceDbContext dbContext,
        Guid userId,
        TrustLedgerEntryType entryType,
        Guid? referenceId,
        string description,
        bool isPublic,
        CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        var exists = await dbContext.TrustLedgerEntries
            .AnyAsync(
                e => e.UserId == userId
                     && e.EntryType == entryType
                     && e.ReferenceId == referenceId,
                ct)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        dbContext.TrustLedgerEntries.Add(
            TrustLedgerEntry.Create(userId, entryType, referenceId, description, isPublic));
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class OnEmailVerifiedRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<EmailVerifiedEvent>
{
    public Task Handle(EmailVerifiedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.UserId,
            TrustLedgerEntryType.EmailVerified, null,
            "Email address verified", isPublic: true, ct);
    }
}

public sealed class OnPhoneVerifiedRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<PhoneVerifiedEvent>
{
    public Task Handle(PhoneVerifiedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.UserId,
            TrustLedgerEntryType.PhoneVerified, null,
            "Phone number verified", isPublic: true, ct);
    }
}

public sealed class OnIdentityVerifiedRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<IdentityVerifiedEvent>
{
    public Task Handle(IdentityVerifiedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.UserId,
            TrustLedgerEntryType.IdentityVerified, domainEvent.ProfileId,
            "Government ID verified", isPublic: true, ct);
    }
}

public sealed class OnBackgroundCheckReceivedRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<BackgroundCheckReceivedEvent>
{
    public Task Handle(BackgroundCheckReceivedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // Only a pass raises the trust level. Review/fail outcomes are kept
        // out of the ledger (consumer-report data stays in its own module).
        if (domainEvent.Result != BackgroundCheckResult.Pass)
        {
            return Task.CompletedTask;
        }

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.UserId,
            TrustLedgerEntryType.BackgroundCheckPassed, domainEvent.ReportId,
            "Background check passed", isPublic: true, ct);
    }
}

public sealed class OnPartnerEndorsementApprovedRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<PartnerEndorsementApprovedEvent>
{
    public Task Handle(PartnerEndorsementApprovedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.TenantUserId,
            TrustLedgerEntryType.PartnerEndorsed, domainEvent.EndorsementId,
            $"Endorsed by partner organization {domainEvent.OrganizationName}",
            isPublic: true, ct);
    }
}

public sealed class OnPartnerEndorsementRevokedRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<PartnerEndorsementRevokedEvent>
{
    public Task Handle(PartnerEndorsementRevokedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.TenantUserId,
            TrustLedgerEntryType.PartnerEndorsementRevoked, domainEvent.EndorsementId,
            $"Endorsement revoked by {domainEvent.OrganizationName}: {domainEvent.Reason}",
            isPublic: true, ct);
    }
}

public sealed class OnPartnerEndorsementExpiredRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<PartnerEndorsementExpiredEvent>
{
    public Task Handle(PartnerEndorsementExpiredEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.TenantUserId,
            TrustLedgerEntryType.PartnerEndorsementExpired, domainEvent.EndorsementId,
            $"Endorsement from {domainEvent.OrganizationName} expired",
            isPublic: true, ct);
    }
}

public sealed class OnBookingCancelledRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<BookingCancelledEvent>
{
    public Task Handle(BookingCancelledEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // System auto-cancellations (e.g. payment window elapsed) are not a
        // trust signal against either party.
        if (domainEvent.IsAutoCancel)
        {
            return Task.CompletedTask;
        }

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.CancelledByUserId,
            TrustLedgerEntryType.EarlyTermination, domainEvent.DealId,
            $"Booking cancelled: {domainEvent.Reason}", isPublic: false, ct);
    }
}

public sealed class OnArbitrationRulingRecordLedgerEntryHandler(ComplianceDbContext dbContext)
    : IDomainEventHandler<ArbitrationRulingIssuedEvent>
{
    public Task Handle(ArbitrationRulingIssuedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return TrustLedgerRecorder.RecordOnceAsync(
            dbContext, domainEvent.PartyUserId,
            TrustLedgerEntryType.ArbitrationRuling, domainEvent.CaseId,
            $"Arbitration ruling issued with penalties: {domainEvent.PenaltySummary}",
            isPublic: false, ct);
    }
}
