using Lagedra.Infrastructure.External.Sms;
using Lagedra.Modules.Notifications.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.Notifications.Presentation.Endpoints;

/// <summary>
/// Receives Twilio's per-message status callbacks (set via StatusCallback in
/// TwilioSmsService). Requests are authenticated with the X-Twilio-Signature
/// header — HMAC-SHA1 over the exact callback URL + form fields, keyed by
/// the account auth token.
/// </summary>
public static class TwilioWebhookEndpoints
{
    public static IEndpointRouteBuilder MapTwilioWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(TwilioWebhookPaths.SmsStatus, HandleSmsStatus)
            .AllowAnonymous()
            .WithTags("Webhooks")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> HandleSmsStatus(
        HttpRequest request,
        IMediator mediator,
        IOptions<TwilioSettings> twilioSettings,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var baseUrl = configuration["App:BaseUrl"];
        var authToken = twilioSettings.Value.AuthToken;
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(authToken))
        {
            // Signature can't be verified without the exact public URL and
            // the account auth token — reject rather than trust blindly.
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var form = await request.ReadFormAsync(ct).ConfigureAwait(false);

        if (!Uri.TryCreate($"{baseUrl.TrimEnd('/')}{TwilioWebhookPaths.SmsStatus}", UriKind.Absolute, out var callbackUri))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var signature = request.Headers["X-Twilio-Signature"].ToString();
        var fields = form.Select(f => new KeyValuePair<string, string>(f.Key, f.Value.ToString()));

        if (!TwilioRequestValidator.IsValid(callbackUri, fields, signature, authToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var messageSid = form["MessageSid"].ToString();
        var messageStatus = form["MessageStatus"].ToString();
        if (string.IsNullOrWhiteSpace(messageSid) || string.IsNullOrWhiteSpace(messageStatus))
        {
            return Results.BadRequest();
        }

        var errorCode = form.TryGetValue("ErrorCode", out var errorCodeValue)
            ? errorCodeValue.ToString()
            : null;

        await mediator
            .Send(new RecordSmsDeliveryStatusCommand(messageSid, messageStatus, errorCode), ct)
            .ConfigureAwait(false);

        return Results.NoContent();
    }
}
