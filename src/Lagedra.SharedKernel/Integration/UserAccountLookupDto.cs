namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Minimal account snapshot for cross-module lookups (e.g. selecting a home
/// owner on a property-manager listing). Does not include profile PII beyond
/// display name and email.
/// </summary>
public sealed record UserAccountLookupDto(
    Guid UserId,
    string DisplayName,
    string Email);
