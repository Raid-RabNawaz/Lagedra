using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Application.Queries;
using Lagedra.Modules.IdentityAndVerification.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/identity")
            .WithTags("Identity")
            .RequireAuthorization();

        group.MapPost("/kyc/start", StartKyc);
        group.MapPost("/kyc/complete", CompleteKyc);
        group.MapGet("/status", GetStatus);

        return app;
    }

    private static async Task<IResult> StartKyc(
        [FromBody] StartKycRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new StartKycCommand(request.UserId, request.FirstName, request.LastName, request.DateOfBirth),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Created($"/v1/identity/status?userId={result.Value.UserId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CompleteKyc(
        [FromBody] CompleteKycRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CompleteKycCommand(request.UserId, request.PersonaInquiryId),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetStatus(
        [FromQuery] Guid userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetVerificationStatusQuery(userId),
            cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}

public sealed record CompleteKycRequest(Guid UserId, string? PersonaInquiryId);
