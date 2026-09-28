using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.LeaseAgreements.Application;

public class GenerateDealLeasePdfCommandTests
{
    private readonly Guid _dealId = Guid.NewGuid();
    private readonly ILeaseAgreementFiller _filler = Substitute.For<ILeaseAgreementFiller>();
    private readonly ILeasePdfGenerator _pdfGenerator = Substitute.For<ILeasePdfGenerator>();
    private readonly IDealLeaseDocumentStore _store = Substitute.For<IDealLeaseDocumentStore>();
    private readonly IDealApplicationStatusProvider _deals = Substitute.For<IDealApplicationStatusProvider>();
    private readonly IListingProvider _listings = Substitute.For<IListingProvider>();
    private readonly IObjectStorageService _storage = Substitute.For<IObjectStorageService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IDealLeaseSignatureStore _signatures = Substitute.For<IDealLeaseSignatureStore>();

    public GenerateDealLeasePdfCommandTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc));
        _signatures.ListByDealAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DealLeaseSignature>());
    }

    [Fact]
    public async Task Returns_stored_pdf_when_fill_throws()
    {
        var existing = StoredPdf();
        _store.GetByDealIdAsync(_dealId, Arg.Any<CancellationToken>()).Returns(existing);
        _deals.GetDealDetailsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns((DealApplicationDetailsDto?)null);
        _filler.FillForDealAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<FilledLeaseAgreement>(
                new InvalidOperationException("Deal not found.")));

        var result = await CreateHandler().Handle(new GenerateDealLeasePdfCommand(_dealId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(existing);
        _pdfGenerator.DidNotReceiveWithAnyArgs().Generate(default!, default!);
    }

    [Fact]
    public async Task Returns_stored_pdf_when_required_fields_are_missing()
    {
        var existing = StoredPdf();
        var templateVersionId = existing.TemplateVersionId ?? Guid.NewGuid();
        _store.GetByDealIdAsync(_dealId, Arg.Any<CancellationToken>()).Returns(existing);
        _deals.GetDealDetailsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns((DealApplicationDetailsDto?)null);
        _filler.FillForDealAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(new FilledLeaseAgreement(
                existing.TemplateId ?? Guid.NewGuid(),
                templateVersionId,
                1,
                "US-CA",
                "California Residential Lease",
                "<p>incomplete</p>",
                new Dictionary<string, string>(),
                ["listing.fullAddress"]));

        var result = await CreateHandler().Handle(new GenerateDealLeasePdfCommand(_dealId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(existing);
        _pdfGenerator.DidNotReceiveWithAnyArgs().Generate(default!, default!);
    }

    [Fact]
    public async Task Skips_generation_when_the_listing_has_no_lease()
    {
        var listingId = Guid.NewGuid();
        _store.GetByDealIdAsync(_dealId, Arg.Any<CancellationToken>()).Returns((DealLeaseDocument?)null);
        _deals.GetDealDetailsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(new DealApplicationDetailsDto(
                Guid.NewGuid(),
                _dealId,
                listingId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 10, 1),
                new DateOnly(2026, 11, 1),
                31,
                300_000,
                300_000,
                0,
                null));
        _listings.GetListingDetailsAsync(listingId, Arg.Any<CancellationToken>())
            .Returns(new ListingDetailsDto(
                listingId,
                Guid.NewGuid(),
                30,
                180,
                300_000,
                300_000,
                "US-CA",
                LeaseAgreementSource: "None"));

        var result = await CreateHandler().Handle(new GenerateDealLeasePdfCommand(_dealId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.NotUsed");
        await _filler.DidNotReceiveWithAnyArgs().FillForDealAsync(default, default);
    }

    [Fact]
    public async Task Fails_when_fill_throws_and_no_stored_pdf_exists()
    {
        _store.GetByDealIdAsync(_dealId, Arg.Any<CancellationToken>()).Returns((DealLeaseDocument?)null);
        _deals.GetDealDetailsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns((DealApplicationDetailsDto?)null);
        _filler.FillForDealAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<FilledLeaseAgreement>(
                new InvalidOperationException("Deal not found.")));

        var result = await CreateHandler().Handle(new GenerateDealLeasePdfCommand(_dealId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseTemplate.FillFailed");
    }

    [Fact]
    public async Task Regenerates_when_a_signature_is_newer_than_the_stored_pdf()
    {
        var existing = StoredPdf();
        var templateVersionId = existing.TemplateVersionId ?? Guid.NewGuid();
        var templateId = existing.TemplateId ?? Guid.NewGuid();
        _store.GetByDealIdAsync(_dealId, Arg.Any<CancellationToken>()).Returns(existing);
        _deals.GetDealDetailsAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns((DealApplicationDetailsDto?)null);
        _filler.FillForDealAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(new FilledLeaseAgreement(
                templateId,
                templateVersionId,
                1,
                "US-CA",
                "California Residential Lease",
                "<p>signed</p>",
                new Dictionary<string, string>(),
                []));
        _signatures.ListByDealAsync(_dealId, Arg.Any<CancellationToken>())
            .Returns(
            [
                DealLeaseSignature.Create(
                    _dealId,
                    DealLeaseSignerRole.Host,
                    Guid.NewGuid(),
                    "Jane Host",
                    "ABC",
                    existing.GeneratedAtUtc.AddMinutes(1)),
            ]);
        _pdfGenerator.Generate(Arg.Any<string>(), Arg.Any<string>()).Returns([0x25, 0x50, 0x44, 0x46, 0x2D]);

        var result = await CreateHandler().Handle(new GenerateDealLeasePdfCommand(_dealId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeSameAs(existing);
        _pdfGenerator.Received(1).Generate("California Residential Lease", "<p>signed</p>");
        await _store.Received(1).SaveAsync(Arg.Any<DealLeaseDocument>(), Arg.Any<CancellationToken>());
    }

    private GenerateDealLeasePdfCommandHandler CreateHandler() =>
        new(
            _filler,
            _pdfGenerator,
            _store,
            _deals,
            _listings,
            _storage,
            Options.Create(new MinioSettings { Endpoint = "localhost" }),
            _clock,
            _signatures);

    private DealLeaseDocument StoredPdf() =>
        new(
            _dealId,
            SnapshotId: null,
            TemplateId: Guid.NewGuid(),
            TemplateVersionId: Guid.NewGuid(),
            FileName: "lease-US-CA-deal.pdf",
            ContentType: "application/pdf",
            Content: [0x25, 0x50, 0x44, 0x46],
            ContentHash: "ABC",
            GeneratedAtUtc: _clock.UtcNow);
}
