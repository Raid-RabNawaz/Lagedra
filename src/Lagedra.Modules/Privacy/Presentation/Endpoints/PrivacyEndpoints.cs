using Lagedra.Modules.Privacy.Application.Commands;
using Lagedra.Modules.Privacy.Application.Queries;
using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.Modules.Privacy.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Privacy.Presentation.Endpoints;

public static class PrivacyEndpoints
{
    public static IEndpointRouteBuilder MapPrivacyEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/privacy")
            .WithTags("Privacy")
            .RequireAuthorization();

        group.MapPost("/consent", RecordConsent);
        group.MapDelete("/consent/{type}", WithdrawConsent);
        group.MapPost("/export", EnqueueDataExport);
        group.MapGet("/export/{id:guid}", GetDataExportStatus);
        group.MapPost("/deletion", RequestDeletion);
        group.MapGet("/consents/{userId:guid}", GetUserConsents);
        group.MapPost("/legal-holds", ApplyLegalHold);
        group.MapDelete("/legal-holds/{id:guid}", ReleaseLegalHold);
        group.MapGet("/legal-holds", ListActiveLegalHolds);

        return app;
    }

    private static async Task<IResult> RecordConsent(
        [FromBody] ConsentRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RecordConsentCommand(request.UserId, request.ConsentType, request.IpAddress, request.UserAgent),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> WithdrawConsent(
        [FromRoute] ConsentType type,
        [FromQuery] Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new WithdrawConsentCommand(userId, type), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> EnqueueDataExport(
        [FromBody] Contracts.DataExportRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new EnqueueDataExportCommand(request.UserId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/privacy/export/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetDataExportStatus(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetDataExportStatusQuery(id), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RequestDeletion(
        [FromBody] Contracts.DeletionRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RequestDeletionCommand(request.UserId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/privacy/deletion/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetUserConsents(
        [FromRoute] Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetUserConsentsQuery(userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ApplyLegalHold(
        [FromBody] ApplyLegalHoldRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ApplyLegalHoldCommand(request.UserId, request.Reason), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/privacy/legal-holds/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ReleaseLegalHold(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ReleaseLegalHoldCommand(id), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ListActiveLegalHolds(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListActiveLegalHoldsQuery(), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
