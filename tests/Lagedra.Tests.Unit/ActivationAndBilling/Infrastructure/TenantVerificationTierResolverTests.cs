using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Infrastructure;

public class TenantVerificationTierResolverTests
{
    private readonly IPartnerEndorsementProvider _partners = Substitute.For<IPartnerEndorsementProvider>();
    private readonly IVerificationSignalProvider _signals = Substitute.For<IVerificationSignalProvider>();
    private readonly ICompletedStayCountProvider _stays = Substitute.For<ICompletedStayCountProvider>();
    private readonly IHostToGuestReputationProvider _reviews = Substitute.For<IHostToGuestReputationProvider>();
    private readonly IUserViolationCountProvider _violations = Substitute.For<IUserViolationCountProvider>();
    private readonly IArbitrationStandingProvider _arbitration = Substitute.For<IArbitrationStandingProvider>();
    private readonly IBookingRestrictionProvider _restrictions = Substitute.For<IBookingRestrictionProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private static readonly DateTime Now = new(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc);

    public TenantVerificationTierResolverTests()
    {
        _clock.UtcNow.Returns(Now);
        _partners.GetActiveEndorsementsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveEndorsementInfo>());
        _stays.GetClosedStayCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(0);
        _reviews.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new HostToGuestReputationDto(0, 0));
        _violations.GetActiveViolationCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(0);
        _arbitration.HasUnresolvedCaseAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _arbitration.HasPlatformBanAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _restrictions.IsBlockedFromBookingAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    private TenantVerificationTierResolver CreateSut() =>
        new(
            _partners,
            _signals,
            _stays,
            _reviews,
            _violations,
            _arbitration,
            _restrictions,
            Options.Create(new TrustTiersOptions()),
            _clock);

    private static VerificationSignalDto IdentityOnly() =>
        new(true, false, false, false, false, false, null);

    [Fact]
    public async Task Identity_only_resolves_Verified()
    {
        _signals.GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(IdentityOnly());

        var result = await CreateSut().ResolveAsync(Guid.NewGuid());

        result.Tier.Should().Be(TenantVerificationTier.Verified);
        result.PartnerOrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task Screening_pass_alone_stays_Verified()
    {
        _signals.GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new VerificationSignalDto(true, false, false, true, false, false, Now));

        var result = await CreateSut().ResolveAsync(Guid.NewGuid());

        result.Tier.Should().Be(TenantVerificationTier.Verified);
    }

    [Fact]
    public async Task Stays_and_reviews_resolve_Trusted()
    {
        _signals.GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(IdentityOnly());
        _stays.GetClosedStayCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(2);
        _reviews.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new HostToGuestReputationDto(2, 4.2));

        var result = await CreateSut().ResolveAsync(Guid.NewGuid());

        result.Tier.Should().Be(TenantVerificationTier.Trusted);
    }

    [Fact]
    public async Task Expired_screening_pass_does_not_count()
    {
        _signals.GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new VerificationSignalDto(
                true, false, false, true, false, false,
                Now.AddDays(-400)));
        _stays.GetClosedStayCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(2);

        var result = await CreateSut().ResolveAsync(Guid.NewGuid());

        result.Tier.Should().Be(TenantVerificationTier.Verified);
    }

    [Fact]
    public async Task Restriction_resolves_Blocklisted()
    {
        _restrictions.IsBlockedFromBookingAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _signals.GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(IdentityOnly());

        var result = await CreateSut().ResolveAsync(Guid.NewGuid());

        result.Tier.Should().Be(TenantVerificationTier.Blocklisted);
    }

    [Fact]
    public async Task Arbitration_platform_ban_resolves_Blocklisted()
    {
        _arbitration.HasPlatformBanAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _signals.GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(IdentityOnly());

        var result = await CreateSut().ResolveAsync(Guid.NewGuid());

        result.Tier.Should().Be(TenantVerificationTier.Blocklisted);
    }

    [Fact]
    public async Task Elite_history_and_partner_resolves_Elite()
    {
        var org = Guid.NewGuid();
        _partners.GetActiveEndorsementsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<ActiveEndorsementInfo>
            {
                new(Guid.NewGuid(), org, "Org", DateTime.UtcNow, DateTime.UtcNow.AddDays(30))
            });
        _signals.GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(IdentityOnly());
        _stays.GetClosedStayCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(5);
        _reviews.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new HostToGuestReputationDto(4, 4.7));

        var result = await CreateSut().ResolveAsync(Guid.NewGuid());

        result.Tier.Should().Be(TenantVerificationTier.Elite);
        result.PartnerOrganizationId.Should().Be(org);
    }

    [Fact]
    public async Task No_signals_resolves_Unverified()
    {
        _signals.GetSignalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VerificationSignalDto?)null);

        var result = await CreateSut().ResolveAsync(Guid.NewGuid());

        result.Tier.Should().Be(TenantVerificationTier.Unverified);
    }
}
