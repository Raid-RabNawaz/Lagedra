using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.LeaseAgreements.Infrastructure;

public class DealLeasePdfServiceTests
{
    [Fact]
    public async Task GetOrGenerate_falls_back_to_stored_pdf_when_generation_fails()
    {
        var dealId = Guid.NewGuid();
        var stored = new DealLeaseDocument(
            dealId,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "lease.pdf",
            "application/pdf",
            [0x25, 0x50, 0x44, 0x46],
            "ABC",
            DateTime.UtcNow);

        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateDealLeasePdfCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DealLeaseDocument>.Failure(
                new Error("LeaseTemplate.FillFailed", "Deal not found.")));

        var store = Substitute.For<IDealLeaseDocumentStore>();
        store.GetByDealIdAsync(dealId, Arg.Any<CancellationToken>()).Returns(stored);

        var service = new DealLeasePdfService(
            mediator,
            store,
            Substitute.For<ILogger<DealLeasePdfService>>());

        var result = await service.GetOrGenerateAsync(dealId);

        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task GetOrGenerate_returns_null_when_generation_fails_and_nothing_is_stored()
    {
        var dealId = Guid.NewGuid();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateDealLeasePdfCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DealLeaseDocument>.Failure(
                new Error("LeaseTemplate.FillFailed", "Deal not found.")));

        var store = Substitute.For<IDealLeaseDocumentStore>();
        store.GetByDealIdAsync(dealId, Arg.Any<CancellationToken>()).Returns((DealLeaseDocument?)null);

        var service = new DealLeasePdfService(
            mediator,
            store,
            Substitute.For<ILogger<DealLeasePdfService>>());

        var result = await service.GetOrGenerateAsync(dealId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrGenerate_returns_null_when_the_listing_has_no_lease()
    {
        var dealId = Guid.NewGuid();
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateDealLeasePdfCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<DealLeaseDocument>.Failure(
                new Error("LeaseAgreement.NotUsed", "This listing does not use a lease agreement.")));

        var store = Substitute.For<IDealLeaseDocumentStore>();
        store.GetByDealIdAsync(dealId, Arg.Any<CancellationToken>()).Returns((DealLeaseDocument?)null);

        var service = new DealLeasePdfService(
            mediator,
            store,
            Substitute.For<ILogger<DealLeasePdfService>>());

        var result = await service.GetOrGenerateAsync(dealId);

        result.Should().BeNull();
    }
}
