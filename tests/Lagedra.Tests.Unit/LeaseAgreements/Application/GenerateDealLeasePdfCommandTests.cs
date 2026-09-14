using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.LeaseAgreements.Application.Commands;
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

    public GenerateDealLeasePdfCommandTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc));
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

    private GenerateDealLeasePdfCommandHandler CreateHandler() =>
        new(
            _filler,
            _pdfGenerator,
            _store,
            _deals,
            _listings,
            _storage,
            Options.Create(new MinioSettings { Endpoint = "localhost" }),
            _clock);

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
