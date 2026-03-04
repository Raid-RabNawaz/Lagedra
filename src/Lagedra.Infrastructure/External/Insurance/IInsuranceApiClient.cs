namespace Lagedra.Infrastructure.External.Insurance;

public enum PolicyStatus { Active, Expired, Cancelled, PendingVerification }

public sealed record PolicyVerificationResult(bool IsValid, PolicyStatus Status, string? PolicyNumber, DateTime? ExpiresAt);

public interface IInsuranceApiClient
{
    Task<PolicyVerificationResult> VerifyPolicyAsync(string policyNumber, Guid userId, CancellationToken ct = default);
    Task<PolicyStatus> GetPolicyStatusAsync(string policyNumber, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
