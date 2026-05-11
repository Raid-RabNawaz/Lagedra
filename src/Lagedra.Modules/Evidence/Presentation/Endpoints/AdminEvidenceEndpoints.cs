using Lagedra.Modules.Evidence.Application.Commands;
using Lagedra.Modules.Evidence.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Evidence.Presentation.Endpoints;

public static class AdminEvidenceEndpoints
{
    public static IEndpointRouteBuilder MapAdminEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/evidence")
            .WithTags("AdminEvidence")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/scan-queue", GetScanQueue);
        group.MapPost("/uploads/{id:guid}/quarantine", QuarantineUpload);

        return app;
    }

    private static async Task<IResult> GetScanQueue(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEvidenceScanQueueQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> QuarantineUpload(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new QuarantineUploadCommand(id), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
