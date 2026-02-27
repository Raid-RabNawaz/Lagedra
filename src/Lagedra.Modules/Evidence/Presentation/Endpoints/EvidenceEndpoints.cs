using Lagedra.Modules.Evidence.Application.Commands;
using Lagedra.Modules.Evidence.Application.Queries;
using Lagedra.Modules.Evidence.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Evidence.Presentation.Endpoints;

public static class EvidenceEndpoints
{
    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/evidence/manifests")
            .WithTags("Evidence")
            .RequireAuthorization();

        group.MapPost("/", CreateManifest);
        group.MapPost("/{id:guid}/seal", SealManifest);
        group.MapGet("/{id:guid}", GetManifest);

        return app;
    }

    private static async Task<IResult> CreateManifest(
        [FromBody] SubmitManifestRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateEvidenceManifestCommand(request.DealId, request.ManifestType), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/evidence/manifests/{result.Value.ManifestId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> SealManifest(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new SealEvidenceManifestCommand(id), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetManifest(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetManifestQuery(id), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
