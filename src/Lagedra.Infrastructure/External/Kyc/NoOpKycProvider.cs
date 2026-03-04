using Lagedra.SharedKernel.Integration;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.External.Kyc;

public sealed partial class NoOpKycProvider(ILogger<NoOpKycProvider> logger) : IKycProvider
{
    public Task<KycInquiryResult> CreateInquiryAsync(
        Guid userId, KycInquiryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var externalId = $"noop-{Guid.NewGuid():N}";
        LogAutoApproved(logger, userId, externalId);

        return Task.FromResult(new KycInquiryResult(
            externalId,
            SessionToken: null,
            KycInquiryStatus.Completed));
    }

    public Task<KycInquiryStatusResult> GetInquiryStatusAsync(
        string externalInquiryId, CancellationToken ct = default)
    {
        return Task.FromResult(new KycInquiryStatusResult(
            externalInquiryId,
            KycInquiryStatus.Completed));
    }

    public Task<KycBackgroundCheckResult> InitiateBackgroundCheckAsync(
        Guid userId, KycBackgroundCheckRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new KycBackgroundCheckResult(
            $"noop-bg-{Guid.NewGuid():N}",
            KycBackgroundCheckOutcome.Clear));
    }

    public Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "NoOp KYC provider: auto-approved inquiry for user {UserId} — external ID {ExternalId}")]
    private static partial void LogAutoApproved(ILogger logger, Guid userId, string externalId);
}
