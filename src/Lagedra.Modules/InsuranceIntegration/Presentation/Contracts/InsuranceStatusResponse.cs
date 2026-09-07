namespace Lagedra.Modules.InsuranceIntegration.Presentation.Contracts;

public sealed record InsuranceStatusResponse(
    Guid PolicyRecordId,
    Guid DealId,
    string State,
    string? Provider,
    string? PolicyNumber,
    DateTime? VerifiedAt,
    DateTime? ExpiresAt,
    string? CoverageScope,
    string? VerificationId = null,
    string? ScreeningStatus = null,
    string? FlaggedReason = null);
