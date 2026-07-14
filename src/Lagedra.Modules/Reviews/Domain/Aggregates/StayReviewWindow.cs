using Lagedra.Modules.Reviews.Domain.Events;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.Reviews.Domain.Aggregates;

/// <summary>
/// Double-blind review window for a completed stay. Opens when the stay
/// completes; publishes submitted reviews when both sides have submitted or
/// when <see cref="ClosesAt"/> elapses.
/// </summary>
public sealed class StayReviewWindow : AggregateRoot<Guid>
{
    public Guid DealId { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid LandlordUserId { get; private set; }
    public Guid TenantUserId { get; private set; }

    public DateTime OpensAt { get; private set; }
    public DateTime ClosesAt { get; private set; }

    public bool GuestSubmitted { get; private set; }
    public bool HostSubmitted { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    public DateTime? ReminderSentAt { get; private set; }

    private StayReviewWindow() { }

    public static StayReviewWindow Open(
        Guid dealId,
        Guid listingId,
        Guid landlordUserId,
        Guid tenantUserId,
        int windowDays,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfLessThan(windowDays, 1);

        var now = clock.UtcNow;
        var window = new StayReviewWindow
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            ListingId = listingId,
            LandlordUserId = landlordUserId,
            TenantUserId = tenantUserId,
            OpensAt = now,
            ClosesAt = now.AddDays(windowDays),
            CreatedAt = now,
            UpdatedAt = now
        };

        window.AddDomainEvent(new StayReviewWindowOpenedEvent(
            window.Id, dealId, landlordUserId, tenantUserId, window.ClosesAt));

        return window;
    }

    public void MarkGuestSubmitted(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        GuestSubmitted = true;
        UpdatedAt = clock.UtcNow;
    }

    public void MarkHostSubmitted(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        HostSubmitted = true;
        UpdatedAt = clock.UtcNow;
    }

    public bool ShouldPublish(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (IsPublished)
        {
            return false;
        }

        return (GuestSubmitted && HostSubmitted) || clock.UtcNow >= ClosesAt;
    }

    public void MarkPublished(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (IsPublished)
        {
            return;
        }

        IsPublished = true;
        PublishedAt = clock.UtcNow;
        UpdatedAt = PublishedAt.Value;
    }

    public bool IsOpen(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return !IsPublished && clock.UtcNow < ClosesAt;
    }

    /// <summary>
    /// Whether an open window still needs a nudge for at least one party who
    /// has not submitted. Reminders repeat every
    /// <paramref name="reminderIntervalDays"/> until the window closes or
    /// both sides submit.
    /// </summary>
    public bool ReminderDue(IClock clock, int reminderIntervalDays = 3)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentOutOfRangeException.ThrowIfLessThan(reminderIntervalDays, 1);

        if (IsPublished || !IsOpen(clock))
        {
            return false;
        }

        if (GuestSubmitted && HostSubmitted)
        {
            return false;
        }

        // First nudge after the interval from open; then every interval thereafter.
        var anchor = ReminderSentAt ?? OpensAt;
        return clock.UtcNow >= anchor.AddDays(reminderIntervalDays);
    }

    public bool NeedsGuestReminder() => !GuestSubmitted;

    public bool NeedsHostReminder() => !HostSubmitted;

    public void MarkReminderSent(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ReminderSentAt = clock.UtcNow;
        UpdatedAt = ReminderSentAt.Value;
    }
}
