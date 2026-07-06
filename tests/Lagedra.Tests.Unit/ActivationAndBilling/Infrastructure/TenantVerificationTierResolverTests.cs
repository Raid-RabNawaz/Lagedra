using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Infrastructure;

public class TenantVerificationTierResolverTests
{
    private readonly IPartnerEndorsementProvider _partners = Substitute.For<IPartnerEndorsementProvider>();
    private readonly IVerificationSignalProvider _signals = Substitute.For<IVerificationSignalProvider>();

    private TenantVerificationTierResolver CreateSut() => new(_partners, _signals);

    private void NoEndorsements() =>
        _partners
            .GetActiveEndorsementsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveEndorsementInfo>());

    private void Signals(VerificationSignalDto? dto) =>
        _signals
            .GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(dto);

    private static VerificationSignalDto Verified() =>
        new(IsIdentityVerified: true,
            IsIdentityPending: false,
            IsIdentityFailed: false,
            IsBackgroundCheckPassed: true,
            IsBackgroundCheckFailed: false,
            IsBackgroundCheckUnderReview: false);

    [Fact]
    public async Task Partner_endorsement_wins_over_background_check()
    {
        var tenant = Guid.NewGuid();
        var lowOrg = new Guid("11111111-1111-1111-1111-111111111111");
        var highOrg = new Guid("22222222-2222-2222-2222-222222222222");

        _partners
            .GetActiveEndorsementsAsync(tenant, Arg.Any<CancellationToken>())
            .Returns(new List<ActiveEndorsementInfo>
            {
                new(Guid.NewGuid(), highOrg, "High", DateTime.UtcNow, DateTime.UtcNow.AddDays(30)),
                new(Guid.NewGuid(), lowOrg, "Low", DateTime.UtcNow, DateTime.UtcNow.AddDays(30)),
            });
        // Even though identity + background pass, partner is the higher tier.
        Signals(Verified());

        var result = await CreateSut().ResolveAsync(tenant);

        result.Tier.Should().Be(TenantVerificationTier.PartnerGuaranteed);
        // Deterministic pick = lowest org id so it matches the Truth Surface ordering.
        result.PartnerOrganizationId.Should().Be(lowOrg);
    }

    [Fact]
    public async Task Identity_verified_and_background_pass_resolves_BackgroundVerified()
    {
        var tenant = Guid.NewGuid();
        NoEndorsements();
        Signals(Verified());

        var result = await CreateSut().ResolveAsync(tenant);

        result.Tier.Should().Be(TenantVerificationTier.BackgroundVerified);
        result.PartnerOrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task Background_under_review_does_not_count_as_verified()
    {
        var tenant = Guid.NewGuid();
        NoEndorsements();
        Signals(new VerificationSignalDto(
            IsIdentityVerified: true,
            IsIdentityPending: false,
            IsIdentityFailed: false,
            IsBackgroundCheckPassed: false,
            IsBackgroundCheckFailed: false,
            IsBackgroundCheckUnderReview: true));

        var result = await CreateSut().ResolveAsync(tenant);

        result.Tier.Should().Be(TenantVerificationTier.Unverified);
        result.PartnerOrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task Identity_verified_but_no_background_resolves_Unverified()
    {
        var tenant = Guid.NewGuid();
        NoEndorsements();
        Signals(new VerificationSignalDto(
            IsIdentityVerified: true,
            IsIdentityPending: false,
            IsIdentityFailed: false,
            IsBackgroundCheckPassed: false,
            IsBackgroundCheckFailed: false,
            IsBackgroundCheckUnderReview: false));

        var result = await CreateSut().ResolveAsync(tenant);

        result.Tier.Should().Be(TenantVerificationTier.Unverified);
    }

    [Fact]
    public async Task No_signals_at_all_resolves_Unverified()
    {
        var tenant = Guid.NewGuid();
        NoEndorsements();
        Signals(null);

        var result = await CreateSut().ResolveAsync(tenant);

        result.Tier.Should().Be(TenantVerificationTier.Unverified);
    }
}
