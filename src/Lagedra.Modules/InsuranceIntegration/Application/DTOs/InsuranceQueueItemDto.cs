namespace Lagedra.Modules.InsuranceIntegration.Application.DTOs;

public sealed record InsuranceQueueItemDto(
    Guid PolicyRecordId,
    Guid DealId,
    Guid TenantUserId,
    DateTime UnknownSince,
    double HoursRemaining);
