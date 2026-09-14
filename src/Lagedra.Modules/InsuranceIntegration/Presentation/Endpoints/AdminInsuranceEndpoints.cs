using Lagedra.Modules.InsuranceIntegration.Application.Commands;
using Lagedra.Modules.InsuranceIntegration.Application.Queries;
using Lagedra.Modules.InsuranceIntegration.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.InsuranceIntegration.Presentation.Endpoints;

public static class AdminInsuranceEndpoints
{
    public static IEndpointRouteBuilder MapAdminInsuranceEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/insurance")
            .WithTags("AdminInsurance")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/unknown-queue", GetUnknownQueue);
        group.MapPost("/{dealId:guid}/resolve", CompleteVerification);

        return app;
    }

    private static async Task<IResult> GetUnknownQueue(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInsuranceUnknownQueueQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> CompleteVerification(
        [FromRoute] Guid dealId,
        [FromBody] CompleteManualVerificationRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator
            .Send(
                new CompleteManualVerificationCommand(
                    dealId,
                    request.IsActive,
                    request.Provider,
                    request.PolicyNumber),
                ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
