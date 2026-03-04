using Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;
using Lagedra.Modules.AntiAbuseAndIntegrity.Application.Queries;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Presentation.Endpoints;

public static class IntegrityEndpoints
{
    public static IEndpointRouteBuilder MapIntegrityEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/integrity")
            .WithTags("Integrity")
            .RequireAuthorization();

        group.MapGet("/flags/{userId:guid}", GetAbuseFlags);
        group.MapGet("/restrictions/{userId:guid}", GetUserRestrictions);
        group.MapPost("/detect/collusion", DetectCollusion);
        group.MapPost("/restrict", ApplyRestriction);

        return app;
    }

    private static async Task<IResult> GetAbuseFlags(
        [FromRoute] Guid userId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetAbuseFlagsQuery(userId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetUserRestrictions(
        [FromRoute] Guid userId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserRestrictionsQuery(userId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> DetectCollusion(
        [FromBody] DetectCollusionCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ApplyRestriction(
        [FromBody] ApplyAccountRestrictionCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/integrity/restrictions/{result.Value.UserId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
