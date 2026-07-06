using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ActivationAndBilling.Presentation.Endpoints;

/// <summary>
/// Phase 16.10 — anonymous endpoint group for token-gated actions
/// triggered straight from transactional emails. Auth is the HMAC
/// signature on the inbound token, not a JWT cookie/bearer, so a host
/// without an active session can still one-tap approve a booking from
/// their inbox. The frontend `/host/approve` page POSTs the token
/// here on page load.
/// </summary>
public static class ActionEndpoints
{
    public static IEndpointRouteBuilder MapActionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/actions")
            .WithTags("Actions")
            .AllowAnonymous();

        group.MapPost("/approve-application", ApproveApplication);

        return app;
    }

    private static async Task<IResult> ApproveApplication(
        [FromBody] ApproveApplicationByTokenRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(httpContext);

        var result = await mediator.Send(
            new ApproveApplicationByTokenCommand(
                request.Token,
                IpAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent: httpContext.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null),
            ct).ConfigureAwait(true);

        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var payload = new { error = result.Error.Code, detail = result.Error.Description };
        return result.Error.Code switch
        {
            "OneTap.token.expired" or "OneTap.token.invalid_signature"
                or "OneTap.token.malformed" or "OneTap.token.wrong_action"
                or "OneTap.token.missing" =>
                Results.Json(payload, statusCode: StatusCodes.Status401Unauthorized),
            "OneTap.token.alreadyUsed" =>
                Results.Json(payload, statusCode: StatusCodes.Status409Conflict),
            "Application.NotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}

public sealed record ApproveApplicationByTokenRequest(
    string Token);
