using Lagedra.Modules.IdentityAndVerification.Domain.Enums;

namespace Lagedra.Modules.IdentityAndVerification.Application.DTOs;

public sealed record HostStripeStatusDto(
    Guid Id,
    Guid HostUserId,
    string StripeAccountId,
    StripeOnboardingStatus OnboardingStatus,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    HostAccountRequirementStatus TaxStatus,
    HostAccountRequirementStatus BankAccountStatus,
    Uri? OnboardingUrl,
    IReadOnlyList<string>? OutstandingRequirements = null,
    IReadOnlyList<string>? PendingVerification = null,
    bool DetailsSubmitted = false,
    string? DisabledReason = null);
