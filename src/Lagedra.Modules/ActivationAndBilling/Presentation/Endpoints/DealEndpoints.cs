using System.Security.Claims;
using Lagedra.Modules.ActivationAndBilling.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ActivationAndBilling.Presentation.Endpoints;

public static class DealEndpoints
{
    public static IEndpointRouteBuilder MapDealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/deals")
            .WithTags("Deals")
            .RequireAuthorization();

        group.MapGet("/mine", ListMyDeals);

        return app;
    }

    private static async Task<IResult> ListMyDeals(
        [FromQuery] string? phase,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new ListMyDealsQuery(userId, phase), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));
}
