using System.Security.Claims;
using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class HostStripeEndpoints
{
    public static IEndpointRouteBuilder MapHostStripeEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/hosts/stripe")
            .WithTags("HostStripe")
            .RequireAuthorization();

        group.MapPost("/onboard", Onboard);
        group.MapPost("/refresh-link", RefreshLink);
        group.MapGet("/status", GetStatus);

        // Provider-agnostic aliases for host payout UX.
        var payouts = app.MapGroup("/v1/hosts/payouts")
            .WithTags("HostPayouts")
            .RequireAuthorization();

        payouts.MapPost("/start", Onboard);
        payouts.MapPost("/refresh-link", RefreshLink);
        payouts.MapGet("/status", GetStatus);

        return app;
    }

    private static async Task<IResult> Onboard(
        ClaimsPrincipal user,
        IMediator mediator,
        IConfiguration configuration,
        IOptions<StripeSettings> stripeSettings,
        HostStripeOnboardRequest? body,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("Email claim not found.");

        var returnUrl = ResolveConnectUrl(body?.ReturnUrl, stripeSettings.Value.ConnectReturnUrl, configuration);
        if (returnUrl.IsFailure)
        {
            return Results.BadRequest(new { error = returnUrl.Error.Code, detail = returnUrl.Error.Description });
        }

        var refreshUrl = ResolveConnectUrl(
            body?.RefreshUrl ?? body?.ReturnUrl,
            stripeSettings.Value.ConnectRefreshUrl,
            configuration);
        if (refreshUrl.IsFailure)
        {
            return Results.BadRequest(new { error = refreshUrl.Error.Code, detail = refreshUrl.Error.Description });
        }

        var result = await mediator
            .Send(new CreateHostStripeAccountCommand(
                userId,
                email,
                returnUrl.Value,
                refreshUrl.Value), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RefreshLink(
        ClaimsPrincipal user,
        IMediator mediator,
        IConfiguration configuration,
        IOptions<StripeSettings> stripeSettings,
        HostStripeOnboardRequest? body,
        CancellationToken ct)
    {
        var userId = GetUserId(user);

        var returnUrl = ResolveConnectUrl(body?.ReturnUrl, stripeSettings.Value.ConnectReturnUrl, configuration);
        if (returnUrl.IsFailure)
        {
            return Results.BadRequest(new { error = returnUrl.Error.Code, detail = returnUrl.Error.Description });
        }

        var refreshUrl = ResolveConnectUrl(
            body?.RefreshUrl ?? body?.ReturnUrl,
            stripeSettings.Value.ConnectRefreshUrl,
            configuration);
        if (refreshUrl.IsFailure)
        {
            return Results.BadRequest(new { error = refreshUrl.Error.Code, detail = refreshUrl.Error.Description });
        }

        var result = await mediator
            .Send(new RefreshHostOnboardingLinkCommand(
                userId,
                returnUrl.Value,
                refreshUrl.Value), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(new { onboardingUrl = result.Value })
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetStatus(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new SyncHostStripeStatusByUserCommand(userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static Lagedra.SharedKernel.Results.Result<Uri> ResolveConnectUrl(
        Uri? url,
        Uri fallback,
        IConfiguration configuration) =>
        StripeConnectUrlValidator.ValidateOrDefault(url, fallback, configuration);

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));
}
