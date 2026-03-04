namespace Lagedra.Modules.AntiAbuseAndIntegrity.Presentation.Contracts;

public sealed record AbuseFlagResponse(
    Guid Id,
    Guid UserId,
    string FlagType,
    string Severity,
    DateTime FlaggedAt);
