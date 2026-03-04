using System.Security.Claims;
using Lagedra.Modules.Arbitration.Application.Commands;
using Lagedra.Modules.Arbitration.Application.Queries;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Presentation.Contracts;
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

        group.MapPost("/", FileCase);
        group.MapPost("/{caseId:guid}/evidence", AttachEvidence);
        group.MapPost("/{caseId:guid}/evidence-complete", MarkEvidenceComplete);
        group.MapPost("/{caseId:guid}/assign", AssignArbitrator);
        group.MapPost("/{caseId:guid}/decision", IssueDecision);
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
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }

    private static async Task<IResult> AttachEvidence(
        [FromRoute] Guid caseId,
        [FromBody] AttachEvidenceRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new AttachEvidenceCommand(caseId, request.SlotType, request.SubmittedBy, request.EvidenceManifestId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
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
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
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
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> IssueDecision(
        [FromRoute] Guid caseId,
        [FromBody] IssueDecisionRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        if (request.AwardAmount.HasValue)
        {
            var bindingResult = await mediator.Send(
                new IssueBindingAwardCommand(caseId, request.DecisionSummary, request.AwardAmount.Value), ct)
                .ConfigureAwait(true);

            return bindingResult.IsSuccess
                ? Results.Ok(bindingResult.Value)
                : Results.BadRequest(new { error = bindingResult.Error.Code, detail = bindingResult.Error.Description });
        }

        var protocolResult = await mediator.Send(
            new IssueProtocolDecisionCommand(caseId, request.DecisionSummary), ct)
            .ConfigureAwait(true);

        return protocolResult.IsSuccess
            ? Results.Ok(protocolResult.Value)
            : Results.BadRequest(new { error = protocolResult.Error.Code, detail = protocolResult.Error.Description });
    }

    private static async Task<IResult> GetCase(
        [FromRoute] Guid caseId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetCaseQuery(caseId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CloseCase(
        [FromRoute] Guid caseId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CloseCaseCommand(caseId), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> AppealCase(
        [FromRoute] Guid caseId,
        [FromBody] AppealCaseRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new AppealCaseCommand(caseId, userId, request.Reason), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ListCasesByStatus(
        [FromQuery] ArbitrationStatus status,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ListCasesByStatusQuery(status), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
