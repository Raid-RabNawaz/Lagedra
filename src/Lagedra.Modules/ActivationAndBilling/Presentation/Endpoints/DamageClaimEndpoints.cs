using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ActivationAndBilling.Presentation.Endpoints;

public static class DamageClaimEndpoints
{
    public static IEndpointRouteBuilder MapDamageClaimEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/deals/{dealId:guid}/damage-claims")
            .WithTags("Damage Claims")
            .RequireAuthorization();

        group.MapPut("/{claimId:guid}/approve", ApproveClaim)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapPut("/{claimId:guid}/reject", RejectClaim)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapPut("/{claimId:guid}/partial-approve", PartiallyApproveClaim)
            .RequireAuthorization("RequirePlatformAdmin");

        return app;
    }

    private static async Task<IResult> ApproveClaim(
        [FromRoute] Guid dealId,
        [FromRoute] Guid claimId,
        [FromBody] DamageClaimResolutionRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ApproveDamageClaimCommand(dealId, claimId, request.ApprovedAmountCents ?? 0, request.Notes), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RejectClaim(
        [FromRoute] Guid dealId,
        [FromRoute] Guid claimId,
        [FromBody] DamageClaimResolutionRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new RejectDamageClaimCommand(dealId, claimId, request.Notes), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> PartiallyApproveClaim(
        [FromRoute] Guid dealId,
        [FromRoute] Guid claimId,
        [FromBody] DamageClaimResolutionRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new PartiallyApproveDamageClaimCommand(dealId, claimId, request.ApprovedAmountCents ?? 0, request.Notes), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
