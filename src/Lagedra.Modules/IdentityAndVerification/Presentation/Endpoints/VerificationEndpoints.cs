using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Application.Queries;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class VerificationEndpoints
{
    public static IEndpointRouteBuilder MapVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/verification")
            .WithTags("Verification")
            .RequireAuthorization();

        group.MapPost("/background-check/consent", SubmitBackgroundCheckConsent);
        group.MapPost("/affiliation", VerifyAffiliation);
        group.MapPost("/fraud-flag", CreateFraudFlag);
        group.MapGet("/fraud-flags", GetFraudFlags);

        return app;
    }

    private static async Task<IResult> SubmitBackgroundCheckConsent(
        [FromBody] BackgroundCheckConsentRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SubmitBackgroundCheckConsentCommand(request.UserId),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Accepted()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> VerifyAffiliation(
        [FromBody] VerifyAffiliationRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new VerifyInstitutionAffiliationCommand(
                request.UserId, request.OrganizationType,
                request.OrganizationId, request.Method),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CreateFraudFlag(
        [FromBody] CreateFraudFlagRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateFraudFlagCommand(request.UserId, request.Reason, request.Source),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Created($"/v1/verification/fraud-flags?userId={request.UserId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetFraudFlags(
        [FromQuery] Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetFraudFlagsQuery(userId),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}

public sealed record BackgroundCheckConsentRequest(Guid UserId);

public sealed record VerifyAffiliationRequest(
    Guid UserId,
    string? OrganizationType,
    Guid? OrganizationId,
    VerificationMethod Method);

public sealed record CreateFraudFlagRequest(
    Guid UserId,
    string Reason,
    string Source);
