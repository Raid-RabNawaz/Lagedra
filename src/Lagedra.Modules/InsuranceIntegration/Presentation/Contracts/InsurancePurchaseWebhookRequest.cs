namespace Lagedra.Modules.InsuranceIntegration.Presentation.Contracts;

public sealed record InsurancePurchaseWebhookRequest(
    Guid DealId,
    string Provider,
    string PolicyNumber,
    string? CoverageScope,
    DateTime? PolicyExpiresAt,
    string RawPayload);
