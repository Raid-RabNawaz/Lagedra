using Lagedra.Modules.Arbitration.Application.Commands;
using Lagedra.Modules.Arbitration.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Arbitration.Presentation.Endpoints;

public static class AdminArbitrationEndpoints
{
    public static IEndpointRouteBuilder MapAdminArbitrationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/arbitration")
            .WithTags("AdminArbitration")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/backlog", GetBacklog);
        group.MapGet("/caseload", GetCaseload);
        group.MapPost("/cases/{caseId:guid}/assign-auto", AutoAssign);

        return app;
    }

    private static async Task<IResult> GetBacklog(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetArbitrationBacklogQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> GetCaseload(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetArbitratorCaseloadQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> AutoAssign(
        [FromRoute] Guid caseId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new AutoAssignArbitratorCommand(caseId), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(new { arbitratorUserId = result.Value })
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
