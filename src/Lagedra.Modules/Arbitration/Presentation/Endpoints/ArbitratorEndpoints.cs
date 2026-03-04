using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Application.Queries;
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
        ArbitrationCaseRepository repository,
        CancellationToken ct)
    {
        var cases = await repository.GetByArbitratorUserIdAsync(userId, ct).ConfigureAwait(true);

        var dtos = cases.Select(GetCaseQueryHandler.MapToDto).ToList();
        return Results.Ok(dtos);
    }
}
