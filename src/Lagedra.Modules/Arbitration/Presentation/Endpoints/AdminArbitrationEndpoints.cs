using Lagedra.Modules.Arbitration.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        return app;
    }

    private static async Task<IResult> GetBacklog(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetArbitrationBacklogQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }
}
