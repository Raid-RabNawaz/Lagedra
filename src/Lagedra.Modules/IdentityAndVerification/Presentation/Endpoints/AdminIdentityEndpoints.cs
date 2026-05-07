using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class AdminIdentityEndpoints
{
    public static IEndpointRouteBuilder MapAdminIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/identity")
            .WithTags("AdminIdentity")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/manual-queue", GetManualQueue);
        group.MapPost("/manual-queue/{id:guid}/approve", ApproveManual);
        group.MapPost("/manual-queue/{id:guid}/reject", RejectManual);

        return app;
    }

    private static async Task<IResult> GetManualQueue(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPendingManualVerificationsQuery(), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }

    private static async Task<IResult> ApproveManual(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ApproveManualVerificationCommand(id), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RejectManual(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RejectManualVerificationCommand(id), ct).ConfigureAwait(true);
        return result.IsSuccess
            ? Results.Ok()
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
