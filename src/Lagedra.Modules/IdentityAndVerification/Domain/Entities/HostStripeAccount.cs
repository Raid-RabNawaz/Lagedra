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

    /// <summary>
    /// Tax-form (W-9/W-8) verification state, derived from the connected
    /// account's outstanding requirements during status sync.
    /// </summary>
    public HostAccountRequirementStatus TaxStatus { get; private set; }

    /// <summary>
    /// External bank account verification state, derived from payout capability
    /// and outstanding requirements during status sync.
    /// </summary>
    public HostAccountRequirementStatus BankAccountStatus { get; private set; }

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
            TaxStatus = HostAccountRequirementStatus.Unknown,
            BankAccountStatus = HostAccountRequirementStatus.Unknown,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Syncs capability flags and requirement-derived states from the connected
    /// account. The tax/bank inputs are neutral booleans extracted from Stripe's
    /// <c>requirements</c> collections so this domain method owns the mapping to
    /// <see cref="HostAccountRequirementStatus"/>.
    /// </summary>
    public void SyncStatus(
        bool chargesEnabled,
        bool payoutsEnabled,
        bool detailsSubmitted,
        bool hasExternalAccount,
        bool hasOutstandingTaxRequirement,
        bool taxRequirementPastDue,
        bool taxRequirementPendingVerification,
        bool isRestricted,
        bool hasOutstandingBankRequirement,
        bool bankRequirementPastDue,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        ChargesEnabled = chargesEnabled;
        PayoutsEnabled = payoutsEnabled;

        OnboardingStatus = detailsSubmitted && chargesEnabled
            ? StripeOnboardingStatus.Completed
            : detailsSubmitted || isRestricted
                ? StripeOnboardingStatus.Restricted
                : StripeOnboardingStatus.Pending;

        BankAccountStatus = DeriveBankStatus(
            payoutsEnabled,
            detailsSubmitted,
            hasExternalAccount,
            hasOutstandingBankRequirement,
            bankRequirementPastDue);

        TaxStatus = DeriveTaxStatus(
            detailsSubmitted,
            hasOutstandingTaxRequirement,
            taxRequirementPastDue,
            taxRequirementPendingVerification);

        UpdatedAt = clock.UtcNow;
    }

    private static HostAccountRequirementStatus DeriveBankStatus(
        bool payoutsEnabled,
        bool detailsSubmitted,
        bool hasExternalAccount,
        bool hasOutstandingBankRequirement,
        bool bankRequirementPastDue)
    {
        // Only treat bank as "Action needed" when Stripe specifically requires
        // an external_account — not when the account is restricted for other
        // reasons (e.g. TOS acceptance), which previously caused a false loop.
        if (bankRequirementPastDue || (hasOutstandingBankRequirement && !hasExternalAccount))
        {
            return HostAccountRequirementStatus.Restricted;
        }

        if (hasOutstandingBankRequirement)
        {
            return HostAccountRequirementStatus.Pending;
        }

        // Bank can be fine while payouts are disabled for an unrelated
        // requirement (phone, identity document, TOS). Don't flip the bank
        // row back to Pending — that is what made hosts think they had to
        // re-verify payouts they already completed.
        if (payoutsEnabled || (hasExternalAccount && detailsSubmitted))
        {
            return HostAccountRequirementStatus.Verified;
        }

        return hasExternalAccount || detailsSubmitted
            ? HostAccountRequirementStatus.Pending
            : HostAccountRequirementStatus.Unknown;
    }

    private static HostAccountRequirementStatus DeriveTaxStatus(
        bool detailsSubmitted,
        bool hasOutstandingTaxRequirement,
        bool taxRequirementPastDue,
        bool taxRequirementPendingVerification)
    {
        if (taxRequirementPastDue)
        {
            return HostAccountRequirementStatus.Restricted;
        }

        if (taxRequirementPendingVerification || hasOutstandingTaxRequirement)
        {
            return HostAccountRequirementStatus.Pending;
        }

        return detailsSubmitted
            ? HostAccountRequirementStatus.Verified
            : HostAccountRequirementStatus.Unknown;
    }
}
