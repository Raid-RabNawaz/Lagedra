using System.Security.Claims;
using Lagedra.Modules.Arbitration.Application.Queries;
using Lagedra.Modules.Arbitration.Application.Services;
using Lagedra.Modules.Arbitration.Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Arbitration.Presentation.Endpoints;

public static class ArbitratorEndpoints
{
    public static IEndpointRouteBuilder MapArbitratorEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/arbitrators")
            .WithTags("Arbitrators")
            .RequireAuthorization();

        group.MapGet("/{userId:guid}/cases", GetArbitratorCases);

        return app;
    }

    private static async Task<IResult> GetArbitratorCases(
        [FromRoute] Guid userId,
        HttpContext httpContext,
        ArbitrationCaseRepository repository,
        CancellationToken ct)
    {
        var callerId = GetUserId(httpContext);
        var isAdmin = httpContext.User.IsInRole("PlatformAdmin");
        var isSelf = callerId == userId;
        var isArbitratorSelf = isSelf && httpContext.User.IsInRole("Arbitrator");

        if (!isAdmin && !isArbitratorSelf)
        {
            return Results.Json(
                new { error = "Arbitration.Forbidden", detail = "You can only list your own assigned cases." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var cases = await repository.GetByArbitratorUserIdAsync(userId, ct).ConfigureAwait(true);
        var dtos = cases.Select(c => GetCaseQueryHandler.MapToDto(c)).ToList();
        return Results.Ok(dtos);
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }
}
