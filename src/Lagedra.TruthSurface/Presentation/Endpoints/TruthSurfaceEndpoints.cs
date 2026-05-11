using System.Security.Claims;
using Lagedra.SharedKernel.Integration;
using Lagedra.TruthSurface.Application.Commands;
using Lagedra.TruthSurface.Application.Queries;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using Lagedra.TruthSurface.Presentation.Authorization;
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
        group.MapGet("/{snapshotId:guid}/receipt", DownloadReceipt);
        group.MapGet("/by-deal/{dealId:guid}", GetSnapshotByDealId);
        group.MapPost("/from-deal/{dealId:guid}", CreateFromDeal);

        return app;
    }

    private static async Task<IResult> CreateSnapshot(
        [FromBody] CreateSnapshotRequest request,
        ClaimsPrincipal user,
        IDealApplicationStatusProvider dealStatusProvider,
        IMediator mediator,
        CancellationToken ct)
    {
        var access = await TruthSurfaceAccess.AuthorizeByDealAsync(
            request.DealId, user, dealStatusProvider, ct).ConfigureAwait(true);

        if (access.Outcome == TruthSurfaceAccessOutcome.NotFound) return Results.NotFound();
        if (access.Outcome == TruthSurfaceAccessOutcome.Forbidden) return Results.Forbid();

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
        ClaimsPrincipal user,
        TruthSurfaceDbContext dbContext,
        IDealApplicationStatusProvider dealStatusProvider,
        IMediator mediator,
        CancellationToken ct)
    {
        var access = await TruthSurfaceAccess.AuthorizeBySnapshotAsync(
            snapshotId, user, dbContext, dealStatusProvider, ct).ConfigureAwait(true);

        if (access.Outcome == TruthSurfaceAccessOutcome.NotFound) return Results.NotFound();
        if (access.Outcome == TruthSurfaceAccessOutcome.Forbidden) return Results.Forbid();

        // The caller must confirm as their actual party — landlords cannot
        // confirm as tenants and vice versa. Admins can act on behalf of either
        // party for support / dispute handling.
        if (!access.IsAdmin)
        {
            if (request.Party == ConfirmingParty.Landlord && !access.IsLandlord) return Results.Forbid();
            if (request.Party == ConfirmingParty.Tenant && !access.IsTenant) return Results.Forbid();
        }

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
        ClaimsPrincipal user,
        TruthSurfaceDbContext dbContext,
        IDealApplicationStatusProvider dealStatusProvider,
        IMediator mediator,
        CancellationToken ct)
    {
        var access = await TruthSurfaceAccess.AuthorizeBySnapshotAsync(
            snapshotId, user, dbContext, dealStatusProvider, ct).ConfigureAwait(true);

        if (access.Outcome == TruthSurfaceAccessOutcome.NotFound) return Results.NotFound();
        if (access.Outcome == TruthSurfaceAccessOutcome.Forbidden) return Results.Forbid();

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
        ClaimsPrincipal user,
        TruthSurfaceDbContext dbContext,
        IDealApplicationStatusProvider dealStatusProvider,
        IMediator mediator,
        CancellationToken ct)
    {
        var access = await TruthSurfaceAccess.AuthorizeBySnapshotAsync(
            snapshotId, user, dbContext, dealStatusProvider, ct).ConfigureAwait(true);

        if (access.Outcome == TruthSurfaceAccessOutcome.NotFound) return Results.NotFound();
        if (access.Outcome == TruthSurfaceAccessOutcome.Forbidden) return Results.Forbid();

        var result = await mediator.Send(new GetSnapshotQuery(snapshotId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> VerifySnapshot(
        [FromRoute] Guid snapshotId,
        ClaimsPrincipal user,
        TruthSurfaceDbContext dbContext,
        IDealApplicationStatusProvider dealStatusProvider,
        IMediator mediator,
        CancellationToken ct)
    {
        var access = await TruthSurfaceAccess.AuthorizeBySnapshotAsync(
            snapshotId, user, dbContext, dealStatusProvider, ct).ConfigureAwait(true);

        if (access.Outcome == TruthSurfaceAccessOutcome.NotFound) return Results.NotFound();
        if (access.Outcome == TruthSurfaceAccessOutcome.Forbidden) return Results.Forbid();

        var result = await mediator.Send(new VerifySnapshotQuery(snapshotId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> DownloadReceipt(
        [FromRoute] Guid snapshotId,
        ClaimsPrincipal user,
        TruthSurfaceDbContext dbContext,
        IDealApplicationStatusProvider dealStatusProvider,
        IMediator mediator,
        CancellationToken ct)
    {
        var access = await TruthSurfaceAccess.AuthorizeBySnapshotAsync(
            snapshotId, user, dbContext, dealStatusProvider, ct).ConfigureAwait(true);

        if (access.Outcome == TruthSurfaceAccessOutcome.NotFound) return Results.NotFound();
        if (access.Outcome == TruthSurfaceAccessOutcome.Forbidden) return Results.Forbid();

        var callerId = TruthSurfaceAccess.TryGetUserId(user);

        var result = await mediator.Send(
            new GetSnapshotReceiptQuery(snapshotId, callerId), ct).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            return Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
        }

        return Results.File(
            result.Value.Bytes,
            contentType: result.Value.ContentType,
            fileDownloadName: result.Value.FileName);
    }

    private static async Task<IResult> GetSnapshotByDealId(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IDealApplicationStatusProvider dealStatusProvider,
        IMediator mediator,
        CancellationToken ct)
    {
        var access = await TruthSurfaceAccess.AuthorizeByDealAsync(
            dealId, user, dealStatusProvider, ct).ConfigureAwait(true);

        if (access.Outcome == TruthSurfaceAccessOutcome.NotFound) return Results.NotFound();
        if (access.Outcome == TruthSurfaceAccessOutcome.Forbidden) return Results.Forbid();

        var result = await mediator.Send(new GetSnapshotByDealIdQuery(dealId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CreateFromDeal(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IDealApplicationStatusProvider dealStatusProvider,
        IMediator mediator,
        CancellationToken ct)
    {
        var access = await TruthSurfaceAccess.AuthorizeByDealAsync(
            dealId, user, dealStatusProvider, ct).ConfigureAwait(true);

        if (access.Outcome == TruthSurfaceAccessOutcome.NotFound) return Results.NotFound();
        if (access.Outcome == TruthSurfaceAccessOutcome.Forbidden) return Results.Forbid();

        // CreateTruthSurfaceForDealCommandHandler additionally enforces that
        // only the landlord may create the snapshot — the auth check above
        // narrows to deal participants + admin; the handler narrows further.
        var userId = TruthSurfaceAccess.TryGetUserId(user)
            ?? throw new InvalidOperationException("User ID claim not found.");

        var result = await mediator.Send(
            new CreateTruthSurfaceForDealCommand(dealId, userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/truth-surface/{result.Value.SnapshotId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
