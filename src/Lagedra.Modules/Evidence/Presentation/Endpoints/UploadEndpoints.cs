using Lagedra.Modules.Evidence.Application.Commands;
using Lagedra.Modules.Evidence.Application.Queries;
using Lagedra.Modules.Evidence.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using EvidenceHttpExtensions = Lagedra.Modules.Evidence.Presentation.EvidenceHttpExtensions;

namespace Lagedra.Modules.Evidence.Presentation.Endpoints;

public static class UploadEndpoints
{
    private const long MaxUploadBytes = 50L * 1024 * 1024;

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/evidence/uploads")
            .WithTags("Evidence")
            .RequireAuthorization();

        group.MapPost("/request-url", RequestUploadUrl);
        group.MapPost("/{id:guid}/complete", CompleteUpload);
        group.MapGet("/{id:guid}/scan", GetScanStatus);
        group.MapGet("/{id:guid}/download-url", GetDownloadUrl);

        group.MapPost("/direct", DirectUpload)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes));

        return app;
    }

    private static async Task<IResult> RequestUploadUrl(
        [FromBody] RequestUploadUrlRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new RequestUploadUrlCommand(request.ManifestId, request.FileName, request.MimeType), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CompleteUpload(
        [FromRoute] Guid id,
        [FromBody] CompleteUploadRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CompleteUploadCommand(
                request.ManifestId, id, request.OriginalFileName,
                request.StorageKey, request.MimeType, request.FileHashHex), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetScanStatus(
        [FromRoute] Guid id,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetScanStatusQuery(id, EvidenceHttpExtensions.GetCallerContext(httpContext)),
            ct).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            return result.Error.Code == "Evidence.Forbidden"
                ? Results.Json(new { error = result.Error.Code, detail = result.Error.Description }, statusCode: StatusCodes.Status403Forbidden)
                : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetDownloadUrl(
        [FromRoute] Guid id,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetDownloadUrlQuery(id, EvidenceHttpExtensions.GetCallerContext(httpContext)),
            ct).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            return result.Error.Code == "Evidence.Forbidden"
                ? Results.Json(new { error = result.Error.Code, detail = result.Error.Description }, statusCode: StatusCodes.Status403Forbidden)
                : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DirectUpload(
        [FromForm] Guid manifestId,
        IFormFile file,
        IMediator mediator,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "Evidence.EmptyFile", detail = "No file was uploaded." });
        }

        var command = new DirectUploadEvidenceCommand(
            manifestId,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellation => Task.FromResult(file.OpenReadStream()));

        var result = await mediator.Send(command, ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
