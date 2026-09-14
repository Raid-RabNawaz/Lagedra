using FluentAssertions;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Settings;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class TrustTiersPolicyTests
{
    private static readonly TrustTiersOptions Options = new();

    private static TrustTierSignals BaseVerified() =>
        new(
            IsIdentityVerified: true,
            IsBlocklisted: false,
            HasActivePartnerEndorsement: false,
            CompletedStayCount: 0,
            HostToGuestReviewCount: 0,
            HostToGuestAverage: 0,
            OpenViolationCount: 0,
            HasUnresolvedArbitration: false,
            HasRecentScreeningPass: false);

    [Fact]
    public void Blocklisted_wins()
    {
        var signals = BaseVerified() with { IsBlocklisted = true, CompletedStayCount = 10 };
        TrustTiersPolicy.Classify(signals, Options).Should().Be(TenantVerificationTier.Blocklisted);
    }

    [Fact]
    public void No_identity_is_Unverified()
    {
        var signals = BaseVerified() with { IsIdentityVerified = false };
        TrustTiersPolicy.Classify(signals, Options).Should().Be(TenantVerificationTier.Unverified);
    }

    [Fact]
    public void Identity_only_is_Verified()
    {
        TrustTiersPolicy.Classify(BaseVerified(), Options).Should().Be(TenantVerificationTier.Verified);
    }

    [Fact]
    public void Screening_pass_alone_stays_Verified()
    {
        var signals = BaseVerified() with { HasRecentScreeningPass = true };
        TrustTiersPolicy.Classify(signals, Options).Should().Be(TenantVerificationTier.Verified);
    }

    [Fact]
    public void Stays_and_reviews_reach_Trusted_without_screening()
    {
        var signals = BaseVerified() with
        {
            CompletedStayCount = 2,
            HostToGuestReviewCount = 2,
            HostToGuestAverage = 4.2
        };
        TrustTiersPolicy.Classify(signals, Options).Should().Be(TenantVerificationTier.Trusted);
    }

    [Fact]
    public void Stay_plus_screening_pass_reaches_Trusted()
    {
        var signals = BaseVerified() with
        {
            CompletedStayCount = 2,
            HasRecentScreeningPass = true
        };
        TrustTiersPolicy.Classify(signals, Options).Should().Be(TenantVerificationTier.Trusted);
    }

    [Fact]
    public void Open_violation_blocks_Trusted()
    {
        var signals = BaseVerified() with
        {
            CompletedStayCount = 2,
            HostToGuestReviewCount = 2,
            HostToGuestAverage = 4.5,
            OpenViolationCount = 1
        };
        TrustTiersPolicy.Classify(signals, Options).Should().Be(TenantVerificationTier.Verified);
    }

    [Fact]
    public void Elite_needs_history_and_partner()
    {
        var signals = BaseVerified() with
        {
            CompletedStayCount = 5,
            HostToGuestReviewCount = 4,
            HostToGuestAverage = 4.6,
            HasActivePartnerEndorsement = true
        };
        TrustTiersPolicy.Classify(signals, Options).Should().Be(TenantVerificationTier.Elite);
    }

    [Fact]
    public void Partner_without_Elite_history_is_Trusted_if_factors_met()
    {
        var signals = BaseVerified() with
        {
            CompletedStayCount = 2,
            HostToGuestReviewCount = 2,
            HostToGuestAverage = 4.2,
            HasActivePartnerEndorsement = true
        };
        TrustTiersPolicy.Classify(signals, Options).Should().Be(TenantVerificationTier.Trusted);
    }
}
