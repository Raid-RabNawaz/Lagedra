using System.Security.Claims;
using Lagedra.Modules.JurisdictionPacks.Application.Commands;
using Lagedra.Modules.JurisdictionPacks.Application.Queries;
using Lagedra.Modules.JurisdictionPacks.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.JurisdictionPacks.Presentation.Endpoints;

public static class JurisdictionPackEndpoints
{
    public static IEndpointRouteBuilder MapJurisdictionPackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/jurisdiction-packs")
            .WithTags("JurisdictionPacks")
            .RequireAuthorization();

        group.MapPost("/", CreatePack).RequireAuthorization("RequirePlatformAdmin");
        group.MapPut("/{id:guid}/versions/{versionId:guid}", UpdateDraft).RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{id:guid}/versions/{versionId:guid}/request-approval", RequestApproval).RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{id:guid}/versions/{versionId:guid}/approve", ApproveVersion).RequireAuthorization("RequirePackApprover");
        group.MapPost("/{id:guid}/versions/{versionId:guid}/publish", PublishVersion).RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{id:guid}/versions/{versionId:guid}/deprecate", DeprecateVersion).RequireAuthorization("RequirePlatformAdmin");
        group.MapGet("/{code}", GetByJurisdictionCode);
        group.MapGet("/{id:guid}/versions", ListVersions).RequireAuthorization("RequirePackApprover");
        group.MapGet("/{id:guid}/versions/{versionId:guid}", GetVersionDetails).RequireAuthorization("RequirePackApprover");

        return app;
    }

    private static async Task<IResult> CreatePack(
        [FromBody] CreatePackVersionRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreatePackDraftCommand(request.JurisdictionCode), cancellationToken)
            .ConfigureAwait(true);

        return result.Match(
            dto => Results.Created($"/v1/jurisdiction-packs/{dto.PackId}", dto),
            err => Results.BadRequest(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> UpdateDraft(
        [FromRoute] Guid id,
        [FromRoute] Guid versionId,
        [FromBody] UpdatePackDraftRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdatePackDraftCommand(
                id, versionId,
                request.EffectiveDate,
                request.EffectiveDateRules,
                request.FieldGatingRules,
                request.EvidenceSchedules,
                request.DepositCapRules), cancellationToken)
            .ConfigureAwait(true);

        return result.Match(
            dto => Results.Ok(dto),
            err => Results.BadRequest(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> RequestApproval(
        [FromRoute] Guid id,
        [FromRoute] Guid versionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RequestDualControlApprovalCommand(id, versionId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ApproveVersion(
        [FromRoute] Guid id,
        [FromRoute] Guid versionId,
        [FromBody] ApprovePackVersionRequest? request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var approverId = request?.ApproverId ?? GetUserId(httpContext);
        var result = await mediator.Send(
            new ApprovePackVersionCommand(id, versionId, approverId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }

    private static async Task<IResult> PublishVersion(
        [FromRoute] Guid id,
        [FromRoute] Guid versionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new PublishPackVersionCommand(id, versionId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> DeprecateVersion(
        [FromRoute] Guid id,
        [FromRoute] Guid versionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DeprecatePackVersionCommand(id, versionId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetByJurisdictionCode(
        [FromRoute] string code,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetActivePackForJurisdictionQuery(code), cancellationToken)
            .ConfigureAwait(true);

        return result.Match(
            dto => Results.Ok(dto),
            err => Results.NotFound(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> ListVersions(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListPackVersionsQuery(id), cancellationToken)
            .ConfigureAwait(true);

        return result.Match(
            dto => Results.Ok(dto),
            err => Results.NotFound(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> GetVersionDetails(
        [FromRoute] Guid id,
        [FromRoute] Guid versionId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetPackVersionDetailsQuery(id, versionId), cancellationToken)
            .ConfigureAwait(true);

        return result.Match(
            dto => Results.Ok(dto),
            err => Results.NotFound(new { error = err.Code, detail = err.Description }));
    }
}
