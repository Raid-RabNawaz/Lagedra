using Lagedra.Modules.Analytics.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Analytics.Presentation.Endpoints;

public static class AdminAnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAdminAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/analytics")
            .WithTags("AdminAnalytics")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/summary", GetSummary);
        group.MapGet("/listings", GetListingAnalytics);

        return app;
    }

    private static async Task<IResult> GetSummary(
        IMediator mediator,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetPlatformSummaryQuery(startDate, endDate), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> GetListingAnalytics(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetListingAnalyticsQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }
}
