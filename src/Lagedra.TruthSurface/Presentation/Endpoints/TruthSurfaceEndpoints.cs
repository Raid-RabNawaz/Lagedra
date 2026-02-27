using Lagedra.TruthSurface.Application.Commands;
using Lagedra.TruthSurface.Application.Queries;
using Lagedra.TruthSurface.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.TruthSurface.Presentation.Endpoints;

public static class TruthSurfaceEndpoints
{
    public static IEndpointRouteBuilder MapTruthSurfaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/truth-surface")
            .WithTags("TruthSurface")
            .RequireAuthorization();

        group.MapPost("/", CreateSnapshot);
        group.MapPost("/{snapshotId:guid}/confirm", ConfirmSnapshot);
        group.MapPost("/{snapshotId:guid}/reconfirm", ReconfirmSnapshot);
        group.MapGet("/{snapshotId:guid}", GetSnapshot);
        group.MapGet("/{snapshotId:guid}/verify", VerifySnapshot);

        return app;
    }

    private static async Task<IResult> CreateSnapshot(
        [FromBody] CreateSnapshotRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateSnapshotCommand(request.DealId, request.ProtocolVersion,
                request.JurisdictionPackVersion, request.CanonicalContent), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/truth-surface/{result.Value.SnapshotId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ConfirmSnapshot(
        [FromRoute] Guid snapshotId,
        [FromBody] ConfirmSnapshotRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ConfirmTruthSurfaceCommand(snapshotId, request.Party), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ReconfirmSnapshot(
        [FromRoute] Guid snapshotId,
        [FromBody] ReconfirmSnapshotRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ReconfirmTruthSurfaceCommand(snapshotId,
                request.NewJurisdictionPackVersion, request.UpdatedCanonicalContent, request.Reason), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/truth-surface/{result.Value.SnapshotId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetSnapshot(
        [FromRoute] Guid snapshotId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetSnapshotQuery(snapshotId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> VerifySnapshot(
        [FromRoute] Guid snapshotId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new VerifySnapshotQuery(snapshotId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
