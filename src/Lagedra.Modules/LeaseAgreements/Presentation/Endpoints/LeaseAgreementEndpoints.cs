using System.Security.Claims;
using Lagedra.Modules.LeaseAgreements.Application.Commands;
using Lagedra.Modules.LeaseAgreements.Application.Queries;
using Lagedra.Modules.LeaseAgreements.Presentation.Contracts;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.LeaseAgreements.Presentation.Endpoints;

public static class LeaseAgreementEndpoints
{
    public static IEndpointRouteBuilder MapLeaseAgreementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/lease-agreements")
            .WithTags("LeaseAgreements")
            .RequireAuthorization();

        group.MapGet("/placeholders", GetPlaceholders).RequireAuthorization("RequirePackApprover");
        group.MapGet("/deals/{dealId:guid}/pdf", DownloadDealPdf);
        group.MapPost("/", CreateTemplate).RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{id:guid}/versions", AddVersion).RequireAuthorization("RequirePlatformAdmin");
        group.MapPut("/{id:guid}/versions/{versionId:guid}", UpdateDraft).RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{id:guid}/versions/{versionId:guid}/request-approval", RequestApproval)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{id:guid}/versions/{versionId:guid}/approve", ApproveVersion)
            .RequireAuthorization("RequirePackApprover");
        group.MapPost("/{id:guid}/versions/{versionId:guid}/publish", PublishVersion)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapPost("/{id:guid}/versions/{versionId:guid}/deprecate", DeprecateVersion)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapGet("/{id:guid}/versions", ListVersions).RequireAuthorization("RequirePackApprover");
        group.MapGet("/{id:guid}/versions/{versionId:guid}", GetVersionDetails)
            .RequireAuthorization("RequirePackApprover");
        group.MapGet("/{code}", GetByCode);

        var admin = app.MapGroup("/v1/admin/lease-agreements")
            .WithTags("AdminLeaseAgreements")
            .RequireAuthorization("RequirePackApprover");

        admin.MapGet("/", ListTemplates);
        admin.MapGet("/pending-approvals", ListPending);

        return app;
    }

    private static async Task<IResult> GetPlaceholders(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLeasePlaceholderCatalogQuery(), ct).ConfigureAwait(true);
        return result.Match(Results.Ok, err => Results.BadRequest(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> CreateTemplate(
        [FromBody] CreateLeaseTemplateRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateLeaseTemplateDraftCommand(request.JurisdictionCode, request.Title), ct)
            .ConfigureAwait(true);

        return result.Match(
            dto => Results.Created($"/v1/lease-agreements/{dto.TemplateId}", dto),
            err => Results.BadRequest(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> AddVersion(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new AddLeaseTemplateVersionCommand(id), ct).ConfigureAwait(true);
        return result.Match(
            versionId => Results.Ok(new { versionId }),
            err => Results.BadRequest(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> UpdateDraft(
        [FromRoute] Guid id,
        [FromRoute] Guid versionId,
        [FromBody] UpdateLeaseTemplateDraftRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateLeaseTemplateDraftCommand(id, versionId, request.BodyHtml, request.EffectiveDate, request.Title),
            ct).ConfigureAwait(true);

        return result.Match(
            Results.Ok,
            err => Results.BadRequest(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> RequestApproval(
        [FromRoute] Guid id, [FromRoute] Guid versionId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new RequestLeaseTemplateApprovalCommand(id, versionId), ct)
            .ConfigureAwait(true);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ApproveVersion(
        [FromRoute] Guid id,
        [FromRoute] Guid versionId,
        [FromBody] ApproveLeaseTemplateRequest? request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var approverId = request?.ApproverId ?? GetUserId(httpContext);
        var result = await mediator.Send(
            new ApproveLeaseTemplateVersionCommand(id, versionId, approverId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> PublishVersion(
        [FromRoute] Guid id, [FromRoute] Guid versionId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new PublishLeaseTemplateVersionCommand(id, versionId), ct)
            .ConfigureAwait(true);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> DeprecateVersion(
        [FromRoute] Guid id, [FromRoute] Guid versionId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeprecateLeaseTemplateVersionCommand(id, versionId), ct)
            .ConfigureAwait(true);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetByCode(
        [FromRoute] string code, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetActiveLeaseTemplateQuery(code), ct).ConfigureAwait(true);
        return result.Match(
            Results.Ok,
            err => Results.NotFound(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> ListVersions(
        [FromRoute] Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListLeaseTemplateVersionsQuery(id), ct).ConfigureAwait(true);
        return result.Match(
            Results.Ok,
            err => Results.NotFound(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> GetVersionDetails(
        [FromRoute] Guid id, [FromRoute] Guid versionId, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLeaseTemplateVersionDetailsQuery(id, versionId), ct)
            .ConfigureAwait(true);
        return result.Match(
            Results.Ok,
            err => Results.NotFound(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> ListTemplates(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListLeaseTemplatesQuery(), ct).ConfigureAwait(true);
        return result.Match(Results.Ok, err => Results.BadRequest(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> ListPending(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListPendingLeaseApprovalsQuery(), ct).ConfigureAwait(true);
        return result.Match(Results.Ok, err => Results.BadRequest(new { error = err.Code, detail = err.Description }));
    }

    private static async Task<IResult> DownloadDealPdf(
        [FromRoute] Guid dealId,
        IDealLeaseDocumentStore store,
        CancellationToken ct)
    {
        var doc = await store.GetByDealIdAsync(dealId, ct).ConfigureAwait(true);
        if (doc is null)
        {
            return Results.NotFound(new { error = "LeaseDocument.NotFound", detail = "No lease PDF for this deal." });
        }

        return Results.File(doc.Content, doc.ContentType, doc.FileName);
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }
}
