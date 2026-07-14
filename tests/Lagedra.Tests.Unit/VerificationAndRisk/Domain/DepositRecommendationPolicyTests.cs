using FluentAssertions;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.Modules.VerificationAndRisk.Domain.Policies;
using Xunit;

namespace Lagedra.Tests.Unit.VerificationAndRisk.Domain;

public class DepositRecommendationPolicyTests
{
    private const long Cap = 10_000_00; // $10,000

    [Fact]
    public void Strong_reputation_softly_lowers_deposit_band()
    {
        var (baseLow, baseHigh) = DepositRecommendationPolicy.Recommend(
            VerificationClass.Medium, InsuranceStatus.Active, Cap);

        var (nudgedLow, nudgedHigh) = DepositRecommendationPolicy.Recommend(
            VerificationClass.Medium,
            InsuranceStatus.Active,
            Cap,
            reputationAverage: 4.8,
            reputationReviewCount: 5);

        var expectedNudge = (long)(DepositRecommendationPolicy.ReputationNudgeFraction * Cap);
        nudgedLow.Should().Be(baseLow - expectedNudge);
        nudgedHigh.Should().Be(baseHigh - expectedNudge);
    }

    [Fact]
    public void Weak_reputation_softly_raises_deposit_band()
    {
        var (baseLow, baseHigh) = DepositRecommendationPolicy.Recommend(
            VerificationClass.Medium, InsuranceStatus.Active, Cap);

        var (nudgedLow, nudgedHigh) = DepositRecommendationPolicy.Recommend(
            VerificationClass.Medium,
            InsuranceStatus.Active,
            Cap,
            reputationAverage: 2.0,
            reputationReviewCount: 4);

        var expectedNudge = (long)(DepositRecommendationPolicy.ReputationNudgeFraction * Cap);
        nudgedLow.Should().Be(baseLow + expectedNudge);
        nudgedHigh.Should().Be(baseHigh + expectedNudge);
    }

    [Fact]
    public void Few_reviews_do_not_nudge()
    {
        var nudge = DepositRecommendationPolicy.ResolveReputationNudgeCents(
            Cap, reputationAverage: 5.0, reputationReviewCount: 2);

        nudge.Should().Be(0);
    }
}
