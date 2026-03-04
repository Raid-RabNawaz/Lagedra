using Lagedra.Infrastructure.External.Persona;
using Lagedra.SharedKernel.Integration;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.External.Kyc;

public sealed partial class PersonaKycProvider(
    IPersonaClient personaClient,
    ILogger<PersonaKycProvider> logger) : IKycProvider
{
    public async Task<KycInquiryResult> CreateInquiryAsync(
        Guid userId, KycInquiryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inquiry = await personaClient
            .CreateInquiryAsync(userId, request.Email, ct)
            .ConfigureAwait(false);

        LogInquiryCreated(logger, userId, inquiry.InquiryId);

        return new KycInquiryResult(
            inquiry.InquiryId,
            inquiry.SessionToken,
            MapStatus(inquiry.Status));
    }

    public async Task<KycInquiryStatusResult> GetInquiryStatusAsync(
        string externalInquiryId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalInquiryId);

        var inquiry = await personaClient
            .GetInquiryAsync(externalInquiryId, ct)
            .ConfigureAwait(false);

        return new KycInquiryStatusResult(
            inquiry.InquiryId,
            MapStatus(inquiry.Status));
    }

    public Task<KycBackgroundCheckResult> InitiateBackgroundCheckAsync(
        Guid userId, KycBackgroundCheckRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        LogBackgroundCheckNotSupported(logger, userId);

        return Task.FromResult(new KycBackgroundCheckResult(
            $"persona-bg-{Guid.NewGuid():N}",
            KycBackgroundCheckOutcome.Review));
    }

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        await personaClient.HandleWebhookAsync(payload, signature, ct).ConfigureAwait(false);
    }

    private static KycInquiryStatus MapStatus(PersonaInquiryStatus status) =>
        status switch
        {
            PersonaInquiryStatus.Created => KycInquiryStatus.Created,
            PersonaInquiryStatus.Pending => KycInquiryStatus.Pending,
            PersonaInquiryStatus.Completed => KycInquiryStatus.Completed,
            PersonaInquiryStatus.Failed => KycInquiryStatus.Failed,
            PersonaInquiryStatus.Expired => KycInquiryStatus.Expired,
            _ => KycInquiryStatus.Created
        };

    [LoggerMessage(Level = LogLevel.Information, Message = "Persona KYC inquiry created for user {UserId}: {InquiryId}")]
    private static partial void LogInquiryCreated(ILogger logger, Guid userId, string inquiryId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Persona background check initiation not fully implemented for user {UserId}; returning review status")]
    private static partial void LogBackgroundCheckNotSupported(ILogger logger, Guid userId);
}
