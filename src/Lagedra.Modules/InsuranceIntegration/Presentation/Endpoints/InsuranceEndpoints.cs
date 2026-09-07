using System.Security.Claims;
using Lagedra.Modules.InsuranceIntegration.Application.Commands;
using Lagedra.Modules.InsuranceIntegration.Application.DTOs;
using Lagedra.Modules.InsuranceIntegration.Application.Queries;
using Lagedra.Modules.InsuranceIntegration.Presentation.Contracts;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.InsuranceIntegration.Presentation.Endpoints;

public static class InsuranceEndpoints
{
    public static IEndpointRouteBuilder MapInsuranceEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/deals/{dealId:guid}/insurance")
            .WithTags("Insurance")
            .RequireAuthorization();

        group.MapGet("/", GetInsuranceStatus);
        group.MapPost("/verify", StartVerification);
        group.MapPost("/manual-proof", UploadManualProof);
        group.MapPut("/reservation", ModifyReservation);
        group.MapPost("/rescreen", RescreenVerification);

        return app;
    }

    private static async Task<IResult> GetInsuranceStatus(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInsuranceStatusQuery(dealId), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value))
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> StartVerification(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var tenantUserId = GetUserId(user);

        var result = await mediator.Send(
            new StartInsuranceVerificationCommand(dealId, tenantUserId), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(ToResponse(result.Value))
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UploadManualProof(
        [FromRoute] Guid dealId,
        [FromBody] ManualProofUploadRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadManualProofCommand(dealId, request.DocumentReference), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Accepted()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ModifyReservation(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ModifyTruviReservationCommand(dealId, GetUserId(user), IsPlatformAdmin(user)),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    private static async Task<IResult> RescreenVerification(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RescreenTruviVerificationCommand(dealId, GetUserId(user), IsPlatformAdmin(user)),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result);
    }

    private static IResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.Error.Code switch
        {
            "Insurance.DealNotFound" or "Insurance.NotScreened" =>
                Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description }),
            "Insurance.Forbidden" =>
                Results.Json(
                    new { error = result.Error.Code, detail = result.Error.Description },
                    statusCode: StatusCodes.Status403Forbidden),
            _ => Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description }),
        };
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    private static bool IsPlatformAdmin(ClaimsPrincipal user) =>
        user.IsInRole("PlatformAdmin");

    private static InsuranceStatusResponse ToResponse(InsuranceStatusDto dto) =>
        new(dto.PolicyRecordId, dto.DealId, dto.State.ToString(),
            dto.Provider, dto.PolicyNumber, dto.VerifiedAt,
            dto.ExpiresAt, dto.CoverageScope,
            dto.VerificationId, dto.ScreeningStatus, dto.FlaggedReason);
}
