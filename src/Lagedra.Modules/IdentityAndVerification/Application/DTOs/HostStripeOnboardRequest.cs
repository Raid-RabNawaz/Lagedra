namespace Lagedra.Modules.IdentityAndVerification.Application.DTOs;

/// <summary>
/// Optional callback URLs for Stripe Connect onboarding. When omitted the
/// server falls back to <c>App:FrontendUrl</c> + <c>/app/payout-setup</c>.
/// </summary>
public sealed record HostStripeOnboardRequest(
    Uri? ReturnUrl = null,
    Uri? RefreshUrl = null);
