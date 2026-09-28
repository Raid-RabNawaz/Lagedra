using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.LeaseAgreements.Application.Queries;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.LeaseAgreements.Application;

public class GetDealLeaseSigningStatusQueryTests
{
    private readonly Guid _dealId = Guid.NewGuid();
    private readonly Guid _listingId = Guid.NewGuid();
    private readonly Guid _hostId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly IDealApplicationStatusProvider _deals = Substitute.For<IDealApplicationStatusProvider>();
    private readonly IListingProvider _listings = Substitute.For<IListingProvider>();
    private readonly IDealLeaseSignatureStore _signatures = Substitute.For<IDealLeaseSignatureStore>();

    public GetDealLeaseSigningStatusQueryTests()
    {
        _deals.GetParticipantsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(new DealParticipantsDto(_hostId, _tenantId, _listingId));
        _deals.GetDealDetailsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(new DealApplicationDetailsDto(
                Guid.NewGuid(),
                _dealId,
                _listingId,
                _tenantId,
                _hostId,
                new DateOnly(2026, 10, 1),
                new DateOnly(2026, 11, 1),
                31,
                300_000,
                300_000,
                0,
                null));
        _listings.GetListingDetailsAsync(_listingId, Arg.Any<CancellationToken>())
            .Returns(new ListingDetailsDto(
                _listingId,
                _hostId,
                30,
                180,
                300_000,
                300_000,
                "US-CA",
                LeaseAgreementSource: "LagedraTemplate",
                RequireLeaseSignatures: true));
        _signatures.ListByDealAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DealLeaseSignature>());
    }

    [Fact]
    public async Task Host_can_sign_when_required_and_unsigned()
    {
        var result = await CreateHandler().Handle(
            new GetDealLeaseSigningStatusQuery(_dealId, _hostId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SigningRequired.Should().BeTrue();
        result.Value.CallerRole.Should().Be("Host");
        result.Value.CallerCanSign.Should().BeTrue();
        result.Value.CallerHasSigned.Should().BeFalse();
        result.Value.BothSigned.Should().BeFalse();
    }

    [Fact]
    public async Task Treats_a_null_lease_source_as_lagedra_template()
    {
        _listings.GetListingDetailsAsync(_listingId, Arg.Any<CancellationToken>())
            .Returns(new ListingDetailsDto(
                _listingId,
                _hostId,
                30,
                180,
                300_000,
                300_000,
                "US-CA",
                LeaseAgreementSource: null,
                RequireLeaseSignatures: true));

        var result = await CreateHandler().Handle(
            new GetDealLeaseSigningStatusQuery(_dealId, _tenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SigningRequired.Should().BeTrue();
        result.Value.CallerRole.Should().Be("Tenant");
    }

    [Fact]
    public async Task Reports_both_signed_after_host_and_tenant()
    {
        _signatures.ListByDealAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(
            [
                DealLeaseSignature.Create(
                    _dealId,
                    DealLeaseSignerRole.Host,
                    _hostId,
                    "Jane Host",
                    "HASH",
                    new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc)),
                DealLeaseSignature.Create(
                    _dealId,
                    DealLeaseSignerRole.Tenant,
                    _tenantId,
                    "Jordan Tenant",
                    "HASH",
                    new DateTime(2026, 9, 21, 11, 0, 0, DateTimeKind.Utc)),
            ]);

        var result = await CreateHandler().Handle(
            new GetDealLeaseSigningStatusQuery(_dealId, _tenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BothSigned.Should().BeTrue();
        result.Value.CallerCanSign.Should().BeFalse();
        result.Value.CallerHasSigned.Should().BeTrue();
        result.Value.Host!.TypedName.Should().Be("Jane Host");
        result.Value.Tenant!.TypedName.Should().Be("Jordan Tenant");
    }

    [Fact]
    public async Task Forbids_a_caller_who_is_not_on_the_deal()
    {
        var result = await CreateHandler().Handle(
            new GetDealLeaseSigningStatusQuery(_dealId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.Forbidden");
    }

    private GetDealLeaseSigningStatusQueryHandler CreateHandler() =>
        new(_deals, _listings, _signatures);
}
