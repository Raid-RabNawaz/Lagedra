using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Application;

public sealed class BulkDenyListingsCommandTests
{
    [Fact]
    public async Task Handle_MissingReason_FailsWithoutTouchingListings()
    {
        var handler = new BulkDenyListingsCommandHandler(null!);
        var result = await handler.Handle(
            new BulkDenyListingsCommand([Guid.NewGuid()], Guid.NewGuid(), "   "),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listing.DenyReasonRequired");
    }

    [Fact]
    public async Task Handle_EmptySelection_Fails()
    {
        var handler = new BulkDenyListingsCommandHandler(null!);
        var result = await handler.Handle(
            new BulkDenyListingsCommand([], Guid.NewGuid(), "Photos are too dark."),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listing.BulkDeny.Empty");
    }

    [Fact]
    public async Task Handle_TooManyIds_Fails()
    {
        var handler = new BulkDenyListingsCommandHandler(null!);
        var ids = Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()).ToList();

        var result = await handler.Handle(
            new BulkDenyListingsCommand(ids, Guid.NewGuid(), "Needs more photos."),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listing.BulkDeny.TooMany");
    }

    [Fact]
    public async Task Handle_ReasonOverLimit_Fails()
    {
        var handler = new BulkDenyListingsCommandHandler(null!);
        var result = await handler.Handle(
            new BulkDenyListingsCommand(
                [Guid.NewGuid()],
                Guid.NewGuid(),
                new string('x', 2001)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Listing.DenyReasonTooLong");
    }
}
