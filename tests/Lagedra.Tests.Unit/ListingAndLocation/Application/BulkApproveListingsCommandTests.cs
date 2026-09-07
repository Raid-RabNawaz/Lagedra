using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Application;

public sealed class BulkApproveListingsCommandTests
{
    [Fact]
    public async Task Handle_EmptySelection_Fails()
    {
        var handler = new BulkApproveListingsCommandHandler(null!);
        var result = await handler.Handle(
            new BulkApproveListingsCommand([], Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listing.BulkApprove.Empty");
    }

    [Fact]
    public async Task Handle_TooManyIds_Fails()
    {
        var handler = new BulkApproveListingsCommandHandler(null!);
        var ids = Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()).ToList();

        var result = await handler.Handle(
            new BulkApproveListingsCommand(ids, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listing.BulkApprove.TooMany");
    }
}
