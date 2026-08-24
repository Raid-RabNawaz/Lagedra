using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

/// <summary>
/// Monthly "did you receive rent?" check-in for an active deal. Months 2+
/// rent is paid to the host directly (non-custodial model), so the platform
/// has no payment record of its own — this is the host's attestation. A
/// missed month raises <see cref="RentMissedEvent"/>, which the compliance
/// module records as a PaymentDefault signal.
/// </summary>
public sealed class RentCheckIn : AggregateRoot<Guid>
{
    public Guid DealId { get; private set; }
    public Guid LandlordUserId { get; private set; }

    /// <summary>First day of the rent period this check-in covers.</summary>
    public DateOnly PeriodStart { get; private set; }

    /// <summary>Exclusive end of the rent period (next period start or stay end).</summary>
    public DateOnly PeriodEnd { get; private set; }

    public RentCheckInStatus Status { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public string? Note { get; private set; }

    private RentCheckIn() { }

    public static RentCheckIn Create(
        Guid dealId,
        Guid landlordUserId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (periodEnd <= periodStart)
        {
            throw new ArgumentOutOfRangeException(nameof(periodEnd), "Period end must be after period start.");
        }

        return new RentCheckIn
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            LandlordUserId = landlordUserId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Status = RentCheckInStatus.Pending,
            CreatedAt = clock.UtcNow,
        };
    }

    public void MarkReceived(string? note, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsurePending();

        Status = RentCheckInStatus.Received;
        RespondedAt = clock.UtcNow;
        Note = Normalize(note);
    }

    public void MarkMissed(string? note, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsurePending();

        Status = RentCheckInStatus.Missed;
        RespondedAt = clock.UtcNow;
        Note = Normalize(note);

        AddDomainEvent(new RentMissedEvent(DealId, LandlordUserId, PeriodStart, PeriodEnd));
    }

    private void EnsurePending()
    {
        if (Status != RentCheckInStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Rent check-in has already been answered ('{Status}').");
        }
    }

    private static string? Normalize(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
