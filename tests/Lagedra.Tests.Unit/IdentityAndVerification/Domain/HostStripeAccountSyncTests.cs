using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Time;
using Xunit;

namespace Lagedra.Tests.Unit.IdentityAndVerification.Domain;

public sealed class HostStripeAccountSyncTests
{
    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow => utcNow;
    }

    [Fact]
    public void SyncStatus_DoesNotMarkBankRestricted_WhenOnlyTosIsPastDue()
    {
        var account = HostStripeAccount.Create(
            Guid.NewGuid(),
            "acct_test",
            new FixedClock(DateTime.UtcNow));

        account.SyncStatus(
            chargesEnabled: false,
            payoutsEnabled: false,
            detailsSubmitted: false,
            hasExternalAccount: false,
            hasOutstandingTaxRequirement: false,
            taxRequirementPastDue: false,
            taxRequirementPendingVerification: false,
            isRestricted: true,
            hasOutstandingBankRequirement: true,
            bankRequirementPastDue: true,
            new FixedClock(DateTime.UtcNow));

        Assert.Equal(HostAccountRequirementStatus.Restricted, account.BankAccountStatus);

        account.SyncStatus(
            chargesEnabled: false,
            payoutsEnabled: false,
            detailsSubmitted: false,
            hasExternalAccount: false,
            hasOutstandingTaxRequirement: false,
            taxRequirementPastDue: false,
            taxRequirementPendingVerification: false,
            isRestricted: true,
            hasOutstandingBankRequirement: false,
            bankRequirementPastDue: false,
            new FixedClock(DateTime.UtcNow));

        Assert.Equal(HostAccountRequirementStatus.Unknown, account.BankAccountStatus);
    }

    [Fact]
    public void SyncStatus_MarksBankVerified_WhenPayoutsEnabled()
    {
        var account = HostStripeAccount.Create(
            Guid.NewGuid(),
            "acct_test",
            new FixedClock(DateTime.UtcNow));

        account.SyncStatus(
            chargesEnabled: true,
            payoutsEnabled: true,
            detailsSubmitted: true,
            hasExternalAccount: true,
            hasOutstandingTaxRequirement: false,
            taxRequirementPastDue: false,
            taxRequirementPendingVerification: false,
            isRestricted: false,
            hasOutstandingBankRequirement: false,
            bankRequirementPastDue: false,
            new FixedClock(DateTime.UtcNow));

        Assert.Equal(HostAccountRequirementStatus.Verified, account.BankAccountStatus);
        Assert.Equal(HostAccountRequirementStatus.Verified, account.TaxStatus);
        Assert.Equal(StripeOnboardingStatus.Completed, account.OnboardingStatus);
    }

    [Fact]
    public void SyncStatus_KeepsBankVerified_WhenPayoutsDisabledForUnrelatedRequirement()
    {
        var account = HostStripeAccount.Create(
            Guid.NewGuid(),
            "acct_test",
            new FixedClock(DateTime.UtcNow));

        account.SyncStatus(
            chargesEnabled: false,
            payoutsEnabled: false,
            detailsSubmitted: true,
            hasExternalAccount: true,
            hasOutstandingTaxRequirement: false,
            taxRequirementPastDue: false,
            taxRequirementPendingVerification: false,
            isRestricted: true,
            hasOutstandingBankRequirement: false,
            bankRequirementPastDue: false,
            new FixedClock(DateTime.UtcNow));

        Assert.Equal(HostAccountRequirementStatus.Verified, account.BankAccountStatus);
        Assert.Equal(HostAccountRequirementStatus.Verified, account.TaxStatus);
        Assert.Equal(StripeOnboardingStatus.Restricted, account.OnboardingStatus);
    }

    [Fact]
    public void ReplaceConnectedAccount_resets_status_and_clears_soft_delete()
    {
        var account = HostStripeAccount.Create(
            Guid.NewGuid(),
            "acct_old",
            new FixedClock(DateTime.UtcNow));
        account.IsDeleted = true;
        account.DeletedAt = DateTime.UtcNow;

        account.ReplaceConnectedAccount("acct_new", new FixedClock(DateTime.UtcNow.AddMinutes(1)));

        Assert.Equal("acct_new", account.StripeAccountId);
        Assert.Equal(StripeOnboardingStatus.Pending, account.OnboardingStatus);
        Assert.False(account.ChargesEnabled);
        Assert.False(account.PayoutsEnabled);
        Assert.False(account.IsDeleted);
        Assert.Null(account.DeletedAt);
    }
}
