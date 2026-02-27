using Lagedra.Modules.ListingAndLocation.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ListingAndLocation.Presentation.Endpoints;

public static class ListingDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapListingDefinitionsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/listing-definitions")
            .WithTags("Listing Definitions")
            .AllowAnonymous();

        group.MapGet("/amenities", GetAmenities);
        group.MapGet("/safety-devices", GetSafetyDevices);
        group.MapGet("/considerations", GetConsiderations);

        return app;
    }

    private static async Task<IResult> GetAmenities(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListAmenityDefinitionsQuery(), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetSafetyDevices(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListSafetyDeviceDefinitionsQuery(), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetConsiderations(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListConsiderationDefinitionsQuery(), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
