using Lagedra.Compliance.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Compliance.Presentation.Endpoints;

public static class AdminComplianceEndpoints
{
    public static IEndpointRouteBuilder MapAdminComplianceEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/compliance")
            .WithTags("AdminCompliance")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/violations", GetAllViolations);

        return app;
    }

    private static async Task<IResult> GetAllViolations(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAllViolationsQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }
}
