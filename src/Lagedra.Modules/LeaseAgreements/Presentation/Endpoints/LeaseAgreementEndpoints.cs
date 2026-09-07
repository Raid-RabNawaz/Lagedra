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

        // Group-level auth only: any signed-in user may read a listing's lease
        // before requesting a booking, unlike the deal PDF which is restricted
        // to the parties on that deal.
        group.MapGet("/listings/{listingId:guid}/preview", DownloadListingLeasePreview);
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
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        // Dual-control: the approver is always the authenticated caller. A
        // client-supplied approver id would let one admin forge both approvals.
        var approverId = GetUserId(httpContext);
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
        HttpContext httpContext,
        IDealLeaseDocumentStore store,
        IMediator mediator,
        IDealApplicationStatusProvider dealProvider,
        CancellationToken ct)
    {
        // Only the deal's landlord/tenant (or a platform admin) may download
        // the filled lease — it contains both parties' personal details.
        if (!httpContext.User.IsInRole("PlatformAdmin"))
        {
            var callerId = GetUserId(httpContext);
            var participants = await dealProvider.GetParticipantsAsync(dealId, ct).ConfigureAwait(true);
            if (participants is null
                || (participants.LandlordUserId != callerId && participants.TenantUserId != callerId))
            {
                return Results.Forbid();
            }
        }

        // Always go through the generator rather than serving the stored blob
        // directly. It returns the stored PDF untouched while the deal's
        // template version (or the host's uploaded file) is unchanged, and
        // re-renders when a newer lease template has been published — otherwise
        // deals sealed under an older template stay pinned to it forever.
        var result = await mediator.Send(new GenerateDealLeasePdfCommand(dealId), ct)
            .ConfigureAwait(true);

        if (result.IsSuccess)
        {
            var doc = result.Value;
            return Results.File(doc.Content, doc.ContentType, doc.FileName);
        }

        // Re-rendering can fail on data a previously generated lease no longer
        // has (an unlocked listing address, say). That earlier PDF is still a
        // valid agreement, so serve it instead of failing the download.
        var existing = await store.GetByDealIdAsync(dealId, ct).ConfigureAwait(true);
        if (existing is not null)
        {
            return Results.File(existing.Content, existing.ContentType, existing.FileName);
        }

        return Results.Problem(
            detail: result.Error.Description,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: result.Error.Code,
            extensions: new Dictionary<string, object?>
            {
                ["error"] = result.Error.Code,
            });
    }

    private static async Task<IResult> DownloadListingLeasePreview(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetListingLeasePreviewQuery(listingId), ct)
            .ConfigureAwait(true);

        if (result.IsFailure)
        {
            var payload = new { error = result.Error.Code, detail = result.Error.Description };
            return result.Error.Code == "Listing.NotFound"
                ? Results.NotFound(payload)
                : Results.Problem(
                    detail: result.Error.Description,
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: result.Error.Code);
        }

        var preview = result.Value;
        return Results.File(preview.Content, preview.ContentType, preview.FileName);
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }
}
