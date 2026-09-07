using System.Security.Claims;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Notifications.Presentation.Endpoints;

public static class SmsConsentEndpoints
{
    public static IEndpointRouteBuilder MapSmsConsentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/sms")
            .WithTags("SMS")
            .AllowAnonymous();

        group.MapPost("/consent", RecordConsent);

        return app;
    }

    private static async Task<IResult> RecordConsent(
        [FromBody] RecordSmsConsentRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Guid? userId = null;
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is not null && Guid.TryParse(claim.Value, out var parsed))
        {
            userId = parsed;
        }

        var result = await mediator.Send(
            new RecordSmsConsentCommand(
                request.PhoneNumber,
                request.Consent,
                request.OptedIn,
                string.IsNullOrWhiteSpace(request.Source)
                    ? SmsConsent.SourceWebForm
                    : request.Source,
                userId),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}

public sealed record RecordSmsConsentRequest(
    string PhoneNumber,
    bool Consent,
    bool OptedIn = true,
    string? Source = null);
