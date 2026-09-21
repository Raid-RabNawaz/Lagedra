using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.LeaseAgreements.Application.Queries;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.LeaseAgreements.Application;

public class GetListingLeasePreviewQueryTests
{
    [Fact]
    public async Task Fails_when_the_listing_has_no_lease()
    {
        var listingId = Guid.NewGuid();
        var listings = Substitute.For<IListingProvider>();
        listings.GetListingDetailsAsync(listingId, Arg.Any<CancellationToken>())
            .Returns(new ListingDetailsDto(
                listingId,
                Guid.NewGuid(),
                30,
                180,
                300_000,
                300_000,
                "US-CA",
                LeaseAgreementSource: "None"));

        var filler = Substitute.For<ILeaseAgreementFiller>();
        var handler = new GetListingLeasePreviewQueryHandler(
            listings,
            filler,
            Substitute.For<ILeasePdfGenerator>(),
            Substitute.For<IObjectStorageService>(),
            Options.Create(new MinioSettings { Endpoint = "localhost" }),
            new MemoryCache(new MemoryCacheOptions()));

        var result = await handler.Handle(
            new GetListingLeasePreviewQuery(listingId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LeaseAgreement.NotUsed");
        await filler.DidNotReceiveWithAnyArgs().FillPreviewForListingAsync(default, default);
    }
}
