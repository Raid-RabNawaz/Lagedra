using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ListingAndLocation.Presentation.Endpoints;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/listings")
            .WithTags("Location")
            .RequireAuthorization("RequireLandlord");

        group.MapPost("/{listingId:guid}/approx-location", SetApproxLocation);
        group.MapPost("/{listingId:guid}/lock-address", LockPreciseAddress);

        return app;
    }

    private static async Task<IResult> SetApproxLocation(
        [FromRoute] Guid listingId,
        [FromBody] SetApproxLocationRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SetApproxLocationCommand(listingId, request.Latitude, request.Longitude),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> LockPreciseAddress(
        [FromRoute] Guid listingId,
        [FromBody] LockPreciseAddressRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new LockPreciseAddressOnActivationCommand(
                listingId,
                request.Street,
                request.City,
                request.State,
                request.ZipCode,
                request.Country,
                request.JurisdictionCode),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
