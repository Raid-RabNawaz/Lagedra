using Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;
using Lagedra.Modules.ListingAndLocation.Application.Queries;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ListingAndLocation.Presentation.Endpoints;

public static class AdminListingDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapAdminListingDefinitionsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/listing-definitions")
            .WithTags("Admin - Listing Definitions")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/amenities", GetAllAmenities);
        group.MapPost("/amenities", CreateAmenity);
        group.MapPut("/amenities/{id:guid}", UpdateAmenity);

        group.MapGet("/safety-devices", GetAllSafetyDevices);
        group.MapPost("/safety-devices", CreateSafetyDevice);
        group.MapPut("/safety-devices/{id:guid}", UpdateSafetyDevice);

        group.MapGet("/considerations", GetAllConsiderations);
        group.MapPost("/considerations", CreateConsideration);
        group.MapPut("/considerations/{id:guid}", UpdateConsideration);

        return app;
    }

    private static async Task<IResult> GetAllAmenities(
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListAmenityDefinitionsQuery(ActiveOnly: false), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CreateAmenity(
        [FromBody] CreateAmenityRequest request,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateAmenityDefinitionCommand(request.Name, request.Category, request.IconKey, request.SortOrder),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/admin/listing-definitions/amenities/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UpdateAmenity(
        [FromRoute] Guid id,
        [FromBody] UpdateAmenityRequest request,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateAmenityDefinitionCommand(id, request.Name, request.Category, request.IconKey, request.IsActive, request.SortOrder),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetAllSafetyDevices(
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListSafetyDeviceDefinitionsQuery(ActiveOnly: false), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CreateSafetyDevice(
        [FromBody] CreateSafetyDeviceRequest request,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateSafetyDeviceDefinitionCommand(request.Name, request.IconKey, request.SortOrder),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/admin/listing-definitions/safety-devices/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UpdateSafetyDevice(
        [FromRoute] Guid id,
        [FromBody] UpdateSafetyDeviceRequest request,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateSafetyDeviceDefinitionCommand(id, request.Name, request.IconKey, request.IsActive, request.SortOrder),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetAllConsiderations(
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListConsiderationDefinitionsQuery(ActiveOnly: false), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CreateConsideration(
        [FromBody] CreateConsiderationRequest request,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateConsiderationDefinitionCommand(request.Name, request.IconKey, request.SortOrder),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/admin/listing-definitions/considerations/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UpdateConsideration(
        [FromRoute] Guid id,
        [FromBody] UpdateConsiderationRequest request,
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateConsiderationDefinitionCommand(id, request.Name, request.IconKey, request.IsActive, request.SortOrder),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}

public sealed record CreateAmenityRequest(string Name, AmenityCategory Category, string IconKey, int SortOrder);
public sealed record UpdateAmenityRequest(string Name, AmenityCategory Category, string IconKey, bool IsActive, int SortOrder);
public sealed record CreateSafetyDeviceRequest(string Name, string IconKey, int SortOrder);
public sealed record UpdateSafetyDeviceRequest(string Name, string IconKey, bool IsActive, int SortOrder);
public sealed record CreateConsiderationRequest(string Name, string IconKey, int SortOrder);
public sealed record UpdateConsiderationRequest(string Name, string IconKey, bool IsActive, int SortOrder);
