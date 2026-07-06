using System.Security.Claims;
using Lagedra.Modules.Arbitration.Application.Commands;
using Lagedra.Modules.Arbitration.Application.Queries;
using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Presentation.Contracts;
using Lagedra.Infrastructure.Middleware;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Arbitration.Presentation.Endpoints;

public static class ArbitrationEndpoints
{
    public static IEndpointRouteBuilder MapArbitrationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/arbitration/cases")
            .WithTags("Arbitration")
            .RequireAuthorization();

        group.MapPost("/", FileCase)
            .RequireRateLimiting(RateLimitingSetup.DisputeCapPolicy);
        group.MapPost("/{caseId:guid}/filing-fee/checkout", CreateFilingFeeCheckout);
        group.MapPost("/{caseId:guid}/evidence", AttachEvidence);
        group.MapPost("/{caseId:guid}/evidence-complete", MarkEvidenceComplete)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{caseId:guid}/assign", AssignArbitrator)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{caseId:guid}/begin-review", BeginReview)
            .RequireAuthorization("RequireArbitrator");
        group.MapPost("/{caseId:guid}/decision", IssueDecision)
            .RequireAuthorization("RequireArbitrator");
        group.MapPut("/{caseId:guid}/close", CloseCase)
            .RequireAuthorization("RequireArbitrator");
        group.MapPost("/{caseId:guid}/appeal", AppealCase);
        group.MapGet("/{caseId:guid}", GetCase);
        group.MapGet("/", ListCasesByStatus);

        return app;
    }

    private static async Task<IResult> FileCase(
        [FromBody] FileCaseRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);

        var result = await mediator.Send(
            new FileCaseCommand(request.DealId, userId, request.Tier, request.Category), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/arbitration/cases/{result.Value.CaseId}", result.Value)
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> CreateFilingFeeCheckout(
        [FromRoute] Guid caseId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);

        var result = await mediator.Send(
            new CreateArbitrationFeeCheckoutCommand(caseId, userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> AttachEvidence(
        [FromRoute] Guid caseId,
        [FromBody] AttachEvidenceRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new AttachEvidenceCommand(
                caseId,
                GetCallerContext(httpContext),
                request.SlotType,
                request.EvidenceManifestId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> MarkEvidenceComplete(
        [FromRoute] Guid caseId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new MarkEvidenceCompleteCommand(caseId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> BeginReview(
        [FromRoute] Guid caseId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new BeginReviewCommand(caseId, GetCallerContext(httpContext)), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> AssignArbitrator(
        [FromRoute] Guid caseId,
        [FromBody] AssignArbitratorRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new AssignArbitratorCommand(caseId, request.ArbitratorUserId, request.ConcurrentCaseCount), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> IssueDecision(
        [FromRoute] Guid caseId,
        [FromBody] IssueDecisionRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        DecisionOutcome? outcome = null;
        DecisionSeverity? severity = null;

        if (request.IsStructured)
        {
            if (!Enum.TryParse<DecisionOutcome>(request.Outcome, ignoreCase: true, out var parsedOutcome))
            {
                return Results.BadRequest(new { error = "Arbitration.InvalidOutcome", detail = "Invalid decision outcome." });
            }

            if (!Enum.TryParse<DecisionSeverity>(request.Severity, ignoreCase: true, out var parsedSeverity))
            {
                return Results.BadRequest(new { error = "Arbitration.InvalidSeverity", detail = "Invalid decision severity." });
            }

            outcome = parsedOutcome;
            severity = parsedSeverity;
        }

        IReadOnlyList<DecisionPenaltyInput> penalties = [];
        if (request.Penalties is { Count: > 0 })
        {
            var parsed = new List<DecisionPenaltyInput>();
            foreach (var p in request.Penalties)
            {
                if (!Enum.TryParse<PenaltyType>(p.PenaltyType, ignoreCase: true, out var penaltyType))
                {
                    return Results.BadRequest(new { error = "Arbitration.InvalidPenaltyType", detail = $"Invalid penalty type '{p.PenaltyType}'." });
                }

                parsed.Add(new DecisionPenaltyInput(p.PartyUserId, penaltyType, p.AmountCents, p.Description));
            }

            penalties = parsed;
        }

        var result = await mediator.Send(
            new IssueDecisionCommand(
                caseId,
                GetCallerContext(httpContext),
                request.DecisionSummary,
                request.AwardAmount,
                request.IsStructured,
                outcome,
                severity,
                penalties), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetCase(
        [FromRoute] Guid caseId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetCaseQuery(caseId, GetCallerContext(httpContext)), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> CloseCase(
        [FromRoute] Guid caseId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CloseCaseCommand(caseId, GetCallerContext(httpContext)), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> AppealCase(
        [FromRoute] Guid caseId,
        [FromBody] AppealCaseRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new AppealCaseCommand(caseId, GetCallerContext(httpContext), request.Reason), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static async Task<IResult> ListCasesByStatus(
        [FromQuery] ArbitrationStatus status,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ListCasesByStatusQuery(status, GetCallerContext(httpContext)), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ArbitrationResults.ToErrorResult(result.Error);
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }

    private static ArbitrationUserContext GetCallerContext(HttpContext httpContext) =>
        new(
            GetUserId(httpContext),
            httpContext.User.IsInRole("PlatformAdmin"),
            httpContext.User.IsInRole("Arbitrator"));
}
