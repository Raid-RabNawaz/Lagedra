using Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;
using Lagedra.Modules.AntiAbuseAndIntegrity.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Presentation.Endpoints;

public static class AdminIntegrityEndpoints
{
    public static IEndpointRouteBuilder MapAdminIntegrityEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/integrity")
            .WithTags("AdminIntegrity")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/flags", GetAllFlags);
        group.MapPost("/flags/{id:guid}/resolve", ResolveFlag);
        group.MapGet("/restrictions", GetAllRestrictions);
        group.MapPost("/restrictions", ApplyRestriction);
        group.MapDelete("/restrictions/{id:guid}", RemoveRestriction);

        return app;
    }

    private static async Task<IResult> GetAllFlags(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllFraudFlagsQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> ResolveFlag(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ResolveFraudFlagCommand(id), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetAllRestrictions(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllRestrictionsQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> ApplyRestriction(
        [FromBody] ApplyAccountRestrictionCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Created($"/v1/admin/integrity/restrictions/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RemoveRestriction(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveAccountRestrictionCommand(id), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
