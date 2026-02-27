using Lagedra.Modules.InsuranceIntegration.Application.Commands;
using Lagedra.Modules.InsuranceIntegration.Application.DTOs;
using Lagedra.Modules.InsuranceIntegration.Application.Queries;
using Lagedra.Modules.InsuranceIntegration.Presentation.Contracts;
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
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new StartInsuranceVerificationCommand(dealId, Guid.Empty), cancellationToken)
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

    private static InsuranceStatusResponse ToResponse(InsuranceStatusDto dto) =>
        new(dto.PolicyRecordId, dto.DealId, dto.State.ToString(),
            dto.Provider, dto.PolicyNumber, dto.VerifiedAt,
            dto.ExpiresAt, dto.CoverageScope);
}
