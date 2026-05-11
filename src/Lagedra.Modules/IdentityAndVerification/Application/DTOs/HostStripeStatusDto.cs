using Lagedra.Modules.IdentityAndVerification.Domain.Enums;

namespace Lagedra.Modules.IdentityAndVerification.Application.DTOs;

public sealed record HostStripeStatusDto(
    Guid Id,
    Guid HostUserId,
    string StripeAccountId,
    StripeOnboardingStatus OnboardingStatus,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    Uri? OnboardingUrl);
