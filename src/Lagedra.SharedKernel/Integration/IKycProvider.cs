namespace Lagedra.SharedKernel.Integration;

public interface IKycProvider
{
    Task<KycInquiryResult> CreateInquiryAsync(Guid userId, KycInquiryRequest request, CancellationToken ct = default);
    Task<KycInquiryStatusResult> GetInquiryStatusAsync(string externalInquiryId, CancellationToken ct = default);
    Task<KycBackgroundCheckResult> InitiateBackgroundCheckAsync(Guid userId, KycBackgroundCheckRequest request, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
}

public sealed record KycInquiryRequest(
    string Email,
    string? FirstName = null,
    string? LastName = null,
    DateTime? DateOfBirth = null);

public sealed record KycInquiryResult(
    string ExternalInquiryId,
    string? SessionToken,
    KycInquiryStatus Status);

public sealed record KycInquiryStatusResult(
    string ExternalInquiryId,
    KycInquiryStatus Status);

public enum KycInquiryStatus
{
    Created,
    Pending,
    Completed,
    Failed,
    Expired
}

public sealed record KycBackgroundCheckRequest(
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth);

public sealed record KycBackgroundCheckResult(
    string ExternalReportId,
    KycBackgroundCheckOutcome Outcome);

public enum KycBackgroundCheckOutcome
{
    Clear,
    Review,
    Adverse,
    Error
}
