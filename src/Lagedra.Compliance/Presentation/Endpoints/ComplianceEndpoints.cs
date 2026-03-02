using System.Security.Claims;
using Lagedra.Compliance.Application.Commands;
using Lagedra.Compliance.Application.Queries;
using Lagedra.Compliance.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Compliance.Presentation.Endpoints;

public static class ComplianceEndpoints
{
    public static IEndpointRouteBuilder MapComplianceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/compliance")
            .WithTags("Compliance")
            .RequireAuthorization();

        group.MapPost("/violations", RecordViolation)
            .RequireAuthorization("RequireLandlord");
        group.MapGet("/violations", GetViolationsForDeal);
        group.MapPut("/violations/{id:guid}/resolve", ResolveViolation)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapPut("/violations/{id:guid}/dismiss", DismissViolation)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapPut("/violations/{id:guid}/escalate", EscalateViolation)
            .RequireAuthorization("RequireLandlord");
        group.MapGet("/ledger/user/{userId:guid}", GetUserLedger);
        group.MapGet("/ledger/deal/{dealId:guid}", GetDealLedger);

        return app;
    }

    private static async Task<IResult> RecordViolation(
        [FromBody] RecordViolationRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var reportedBy = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await mediator.Send(
            new RecordViolationCommand(
                request.DealId,
                reportedBy,
                request.TargetUserId,
                request.Category,
                request.Description,
                request.EvidenceReference),
            ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/compliance/violations/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetViolationsForDeal(
        [FromQuery] Guid dealId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetViolationsForDealQuery(dealId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ResolveViolation(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ResolveViolationCommand(id), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> DismissViolation(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new DismissViolationCommand(id), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> EscalateViolation(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new EscalateViolationCommand(id), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetUserLedger(
        [FromRoute] Guid userId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetTrustLedgerForUserQuery(userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetDealLedger(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetFullLedgerForDealQuery(dealId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
