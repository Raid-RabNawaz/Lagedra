using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.External.Insurance;

/// <summary>
/// Stub implementation — real MGA partner integration is TBD.
/// Replace with the actual HTTP client once the partner API contract is confirmed.
/// </summary>
public sealed partial class InsuranceApiClient(ILogger<InsuranceApiClient> logger) : IInsuranceApiClient
{
    public Task<PolicyVerificationResult> VerifyPolicyAsync(string policyNumber, Guid userId, CancellationToken ct = default)
    {
        _ = ct;
        LogStubCalled(logger, nameof(VerifyPolicyAsync), policyNumber);
        return Task.FromResult(new PolicyVerificationResult(
            IsValid: true,
            Status: PolicyStatus.Active,
            PolicyNumber: policyNumber,
            ExpiresAt: DateTime.UtcNow.AddYears(1)));
    }

    public Task<PolicyStatus> GetPolicyStatusAsync(string policyNumber, CancellationToken ct = default)
    {
        _ = ct;
        LogStubCalled(logger, nameof(GetPolicyStatusAsync), policyNumber);
        return Task.FromResult(PolicyStatus.Active);
    }

    public Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        _ = ct;
        _ = payload;
        _ = signature;
        LogStubCalled(logger, nameof(HandleWebhookAsync), "webhook");
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "[STUB] InsuranceApiClient.{Method} called for '{Reference}' — real MGA partner not yet integrated")]
    private static partial void LogStubCalled(ILogger logger, string method, string reference);
}
