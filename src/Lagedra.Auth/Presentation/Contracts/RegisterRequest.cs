using Lagedra.Auth.Domain;

namespace Lagedra.Auth.Presentation.Contracts;

/// <summary>
/// Self sign-up payload for the multi-step join flow.
///
/// <para><see cref="Password"/> is optional on purpose: when the platform is in
/// pre-launch mode the flow is a password-less founding-partner waitlist, so
/// the client omits it. When pre-launch is off a password is required and the
/// server rejects the request without one.</para>
/// </summary>
public sealed record RegisterRequest(
    string Email,
    UserRole Role,
    string? Password = null,
    string? FullName = null,
    string? CompanyName = null,
    string? Phone = null,
    string? City = null,
    string? SignupType = null,
    string? PortfolioSize = null,
    string? HousingType = null,
    string? PlacementsPerYear = null);
