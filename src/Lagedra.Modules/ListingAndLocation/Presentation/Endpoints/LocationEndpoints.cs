using System.Security.Claims;
using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Presentation.Contracts;
using Lagedra.SharedKernel.Results;
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
            .RequireAuthorization("RequireMember");

        group.MapPost("/{listingId:guid}/approx-location", SetApproxLocation);
        group.MapPost("/{listingId:guid}/lock-address", LockPreciseAddress);

        return app;
    }

    private static async Task<IResult> SetApproxLocation(
        [FromRoute] Guid listingId,
        [FromBody] SetApproxLocationRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new SetApproxLocationCommand(listingId, userId, request.Latitude, request.Longitude),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> LockPreciseAddress(
        [FromRoute] Guid listingId,
        [FromBody] LockPreciseAddressRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new LockPreciseAddressOnActivationCommand(
                listingId,
                userId,
                request.Street,
                request.City,
                request.State,
                request.ZipCode,
                request.Country,
                request.JurisdictionCode),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }

    private static IResult ToErrorResult(Error error)
    {
        var payload = new { error = error.Code, detail = error.Description };
        return error.Code switch
        {
            "Listing.Forbidden" => Results.Json(payload, statusCode: StatusCodes.Status403Forbidden),
            "Listing.NotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}
