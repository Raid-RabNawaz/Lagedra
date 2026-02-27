using Lagedra.Modules.Evidence.Application.Commands;
using Lagedra.Modules.Evidence.Application.Queries;
using Lagedra.Modules.Evidence.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Evidence.Presentation.Endpoints;

public static class UploadEndpoints
{
    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/evidence/uploads")
            .WithTags("Evidence")
            .RequireAuthorization();

        group.MapPost("/request-url", RequestUploadUrl);
        group.MapPost("/{id:guid}/complete", CompleteUpload);
        group.MapGet("/{id:guid}/scan", GetScanStatus);

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
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetScanStatusQuery(id), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
