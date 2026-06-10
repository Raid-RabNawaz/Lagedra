using Lagedra.Modules.JurisdictionPacks.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.JurisdictionPacks.Presentation.Endpoints;

public static class AdminJurisdictionPackEndpoints
{
    public static IEndpointRouteBuilder MapAdminJurisdictionPackEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/jurisdiction-packs")
            .WithTags("AdminJurisdictionPacks")
            .RequireAuthorization("RequirePackApprover");

        group.MapGet("/", ListPacks);
        group.MapGet("/pending-approvals", ListPendingApprovals);

        return app;
    }

    private static async Task<IResult> ListPacks(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListJurisdictionPacksQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> ListPendingApprovals(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListPendingPackApprovalsQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }
}
