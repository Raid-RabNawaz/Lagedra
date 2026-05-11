using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Entities;

public sealed class HostStripeAccount : Entity<Guid>
{
    public Guid HostUserId { get; private set; }
    public string StripeAccountId { get; private set; } = string.Empty;
    public StripeOnboardingStatus OnboardingStatus { get; private set; }
    public bool ChargesEnabled { get; private set; }
    public bool PayoutsEnabled { get; private set; }

    private HostStripeAccount() { }

    public static HostStripeAccount Create(
        Guid hostUserId,
        string stripeAccountId,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeAccountId);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new HostStripeAccount
        {
            Id = Guid.NewGuid(),
            HostUserId = hostUserId,
            StripeAccountId = stripeAccountId,
            OnboardingStatus = StripeOnboardingStatus.Pending,
            ChargesEnabled = false,
            PayoutsEnabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SyncStatus(bool chargesEnabled, bool payoutsEnabled, bool detailsSubmitted, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        ChargesEnabled = chargesEnabled;
        PayoutsEnabled = payoutsEnabled;

        OnboardingStatus = detailsSubmitted && chargesEnabled
            ? StripeOnboardingStatus.Completed
            : detailsSubmitted
                ? StripeOnboardingStatus.Restricted
                : StripeOnboardingStatus.Pending;

        UpdatedAt = clock.UtcNow;
    }
}
