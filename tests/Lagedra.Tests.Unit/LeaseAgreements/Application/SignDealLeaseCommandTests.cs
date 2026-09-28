using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Time;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.LeaseAgreements.Application;

public class SignDealLeaseCommandTests
{
    private readonly Guid _dealId = Guid.NewGuid();
    private readonly Guid _listingId = Guid.NewGuid();
    private readonly Guid _hostId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly IDealApplicationStatusProvider _deals = Substitute.For<IDealApplicationStatusProvider>();
    private readonly IListingProvider _listings = Substitute.For<IListingProvider>();
    private readonly IDealLeaseSignatureStore _signatures = Substitute.For<IDealLeaseSignatureStore>();
    private readonly IDealLeasePdfService _pdfs = Substitute.For<IDealLeasePdfService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly List<DealLeaseSignature> _saved = [];

    public SignDealLeaseCommandTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc));
        _deals.GetParticipantsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(new DealParticipantsDto(_hostId, _tenantId, _listingId));
        _deals.GetDealDetailsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(DealDetails());
        _listings.GetListingDetailsAsync(_listingId, Arg.Any<CancellationToken>())
            .Returns(Listing(requireSignatures: true));
        var storedPdf = StoredPdf();
        _pdfs.GetOrGenerateAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(storedPdf);
        _signatures.GetAsync(Arg.Any<Guid>(), Arg.Any<DealLeaseSignerRole>(), Arg.Any<CancellationToken>())
            .Returns(ci => _saved.FirstOrDefault(s => s.Role == ci.Arg<DealLeaseSignerRole>()));
        _signatures.ListByDealAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => _saved.ToList());
        _signatures.When(s => s.SaveAsync(Arg.Any<DealLeaseSignature>(), Arg.Any<CancellationToken>()))
            .Do(ci => _saved.Add(ci.Arg<DealLeaseSignature>()));
    }

    [Fact]
    public async Task Fails_when_the_caller_does_not_accept()
    {
        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _hostId, "Jane Host", Accepted: false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.SignatureNotAccepted");
        await _signatures.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Fact]
    public async Task Fails_when_the_typed_name_is_too_short()
    {
        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _hostId, "J", Accepted: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.SignatureNameRequired");
    }

    [Fact]
    public async Task Fails_when_the_deal_is_missing()
    {
        _deals.GetParticipantsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns((DealParticipantsDto?)null);

        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _hostId, "Jane Host", Accepted: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.DealNotFound");
    }

    [Fact]
    public async Task Fails_when_the_caller_is_not_a_party()
    {
        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, Guid.NewGuid(), "Stranger", Accepted: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.Forbidden");
    }

    [Fact]
    public async Task Fails_when_the_listing_does_not_require_signatures()
    {
        _listings.GetListingDetailsAsync(_listingId, Arg.Any<CancellationToken>())
            .Returns(Listing(requireSignatures: false));

        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _hostId, "Jane Host", Accepted: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.SigningNotRequired");
    }

    [Fact]
    public async Task Fails_when_the_listing_uses_a_host_uploaded_lease()
    {
        _listings.GetListingDetailsAsync(_listingId, Arg.Any<CancellationToken>())
            .Returns(Listing(requireSignatures: true, source: "HostProvided"));

        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _hostId, "Jane Host", Accepted: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.SigningNotRequired");
    }

    [Fact]
    public async Task Fails_when_the_lease_pdf_is_not_ready()
    {
        _pdfs.GetOrGenerateAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns((DealLeaseDocument?)null);

        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _hostId, "Jane Host", Accepted: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.PdfNotReady");
    }

    [Fact]
    public async Task Host_can_sign_and_the_pdf_is_regenerated()
    {
        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _hostId, "  Jane Host  ", Accepted: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SigningRequired.Should().BeTrue();
        result.Value.CallerRole.Should().Be("Host");
        result.Value.CallerHasSigned.Should().BeTrue();
        result.Value.CallerCanSign.Should().BeFalse();
        result.Value.BothSigned.Should().BeFalse();
        result.Value.Host!.TypedName.Should().Be("Jane Host");
        result.Value.Tenant.Should().BeNull();
        _saved.Should().ContainSingle();
        _saved[0].DocumentHash.Should().Be("ABC123");
        await _pdfs.Received(2).GetOrGenerateAsync(_dealId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tenant_can_sign_after_the_host()
    {
        _saved.Add(DealLeaseSignature.Create(
            _dealId,
            DealLeaseSignerRole.Host,
            _hostId,
            "Jane Host",
            "ABC123",
            _clock.UtcNow.AddMinutes(-5)));

        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _tenantId, "Jordan Tenant", Accepted: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CallerRole.Should().Be("Tenant");
        result.Value.BothSigned.Should().BeTrue();
        result.Value.Tenant!.TypedName.Should().Be("Jordan Tenant");
    }

    [Fact]
    public async Task Already_signed_returns_status_without_saving_again()
    {
        _saved.Add(DealLeaseSignature.Create(
            _dealId,
            DealLeaseSignerRole.Host,
            _hostId,
            "Jane Host",
            "ABC123",
            _clock.UtcNow.AddMinutes(-5)));

        var result = await CreateHandler().Handle(
            new SignDealLeaseCommand(_dealId, _hostId, "Jane Host Again", Accepted: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CallerHasSigned.Should().BeTrue();
        _saved.Should().ContainSingle();
        await _pdfs.DidNotReceiveWithAnyArgs().GetOrGenerateAsync(default, default);
        await _signatures.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    private SignDealLeaseCommandHandler CreateHandler() =>
        new(_deals, _listings, _signatures, _pdfs, _clock);

    private DealApplicationDetailsDto DealDetails() =>
        new(
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
            null);

    private ListingDetailsDto Listing(bool requireSignatures, string? source = "LagedraTemplate") =>
        new(
            _listingId,
            _hostId,
            30,
            180,
            300_000,
            300_000,
            "US-CA",
            LeaseAgreementSource: source,
            RequireLeaseSignatures: requireSignatures);

    private DealLeaseDocument StoredPdf() =>
        new(
            _dealId,
            SnapshotId: null,
            TemplateId: Guid.NewGuid(),
            TemplateVersionId: Guid.NewGuid(),
            FileName: "lease-US-CA-deal.pdf",
            ContentType: "application/pdf",
            Content: [0x25, 0x50, 0x44, 0x46],
            ContentHash: "ABC123",
            GeneratedAtUtc: _clock.UtcNow);
}
