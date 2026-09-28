using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Application.Queries;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class AdminIdentityEndpoints
{
    public static IEndpointRouteBuilder MapAdminIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/identity")
            .WithTags("AdminIdentity")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/manual-queue", GetManualQueue);
        group.MapGet("/manual-queue/{id:guid}", GetManualDetail);
        group.MapGet("/manual-queue/{id:guid}/documents/{documentType}", GetManualDocument);
        group.MapPost("/manual-queue/{id:guid}/approve", ApproveManual);
        group.MapPost("/manual-queue/{id:guid}/reject", RejectManual);
        group.MapGet("/fraud-flags", GetAllFraudFlags);
        group.MapPost("/fraud-flags/{id:guid}/resolve", ResolveFraudFlag);

        return app;
    }

    private static async Task<IResult> GetManualDetail(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetManualVerificationDetailQuery(id), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetManualDocument(
        [FromRoute] Guid id,
        [FromRoute] string documentType,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        if (!TryParseDocumentType(documentType, out var parsed))
        {
            return Results.BadRequest(new
            {
                error = "Identity.Kyc.InvalidDocumentType",
                detail = "Unknown document type.",
            });
        }

        var result = await mediator
            .Send(new GetManualVerificationDocumentQuery(id, parsed), ct)
            .ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            var payload = new { error = result.Error.Code, detail = result.Error.Description };
            return result.Error.Code is "Identity.NotFound" or "Identity.Kyc.DocumentNotFound"
                ? Results.NotFound(payload)
                : Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: result.Error.Description);
        }

        http.Response.Headers.CacheControl = "private, no-store";
        http.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        return Results.File(result.Value.Content, result.Value.ContentType);
    }

    private static bool TryParseDocumentType(string? value, out KycDocumentType parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value)
            || !Enum.TryParse(value, ignoreCase: true, out parsed)
            || !Enum.IsDefined(parsed))
        {
            return false;
        }

        var name = Enum.GetName(parsed);
        return name is not null && name.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<IResult> GetManualQueue(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPendingManualVerificationsQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> ApproveManual(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ApproveManualVerificationCommand(id), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RejectManual(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RejectManualVerificationCommand(id), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetAllFraudFlags(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllIdentityFraudFlagsQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> ResolveFraudFlag(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ResolveIdentityFraudFlagCommand(id), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
