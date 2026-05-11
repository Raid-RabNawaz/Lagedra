using Lagedra.Modules.InsuranceIntegration.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.InsuranceIntegration.Presentation.Endpoints;

public static class AdminInsuranceEndpoints
{
    public static IEndpointRouteBuilder MapAdminInsuranceEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/insurance")
            .WithTags("AdminInsurance")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/unknown-queue", GetUnknownQueue);

        return app;
    }

    private static async Task<IResult> GetUnknownQueue(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInsuranceUnknownQueueQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }
}
