namespace Lagedra.Modules.InsuranceIntegration.Presentation.Contracts;

public sealed record CompleteManualVerificationRequest(
    bool IsActive,
    string? Provider = null,
    string? PolicyNumber = null);
