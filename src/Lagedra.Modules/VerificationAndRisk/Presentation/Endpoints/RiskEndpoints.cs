using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Application.DTOs;
using Lagedra.Modules.VerificationAndRisk.Application.Queries;
using Lagedra.Modules.VerificationAndRisk.Presentation.Contracts;

namespace Lagedra.Modules.VerificationAndRisk.Presentation.Endpoints;

public static class RiskEndpoints
{
    public static IEndpointRouteBuilder MapRiskEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/risk")
            .WithTags("Risk")
            .RequireAuthorization();

        group.MapGet("/{tenantUserId:guid}", GetRiskView);
        group.MapPost("/{tenantUserId:guid}/recalculate", Recalculate);

        return app;
    }

    private static async Task<IResult> GetRiskView(
        [FromRoute] Guid tenantUserId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetRiskViewForLandlordQuery(tenantUserId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(MapToResponse(result.Value))
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> Recalculate(
        [FromRoute] Guid tenantUserId,
        [FromBody] RecalculateRiskRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new RecalculateVerificationClassCommand(
                tenantUserId,
                request.IdentityStatus,
                request.BackgroundStatus,
                request.InsuranceStatus,
                request.ViolationCount), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static RiskViewResponse MapToResponse(RiskViewDto dto) =>
        new(dto.TenantUserId,
            dto.VerificationClass.ToString(),
            dto.ConfidenceLevel.ToString(),
            dto.ConfidenceReason,
            dto.DepositBandLowCents,
            dto.DepositBandHighCents,
            dto.ComputedAt);
}
