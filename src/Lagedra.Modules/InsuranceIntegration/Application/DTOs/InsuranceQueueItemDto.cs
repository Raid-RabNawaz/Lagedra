namespace Lagedra.Modules.InsuranceIntegration.Application.DTOs;

public sealed record InsuranceQueueItemDto(
    Guid PolicyRecordId,
    Guid DealId,
    Guid TenantUserId,
    DateTime UnknownSince,
    double HoursRemaining,
    string? TenantEmail = null,
    string? TenantName = null,
    Guid? ListingId = null,
    string? ListingTitle = null,
    DateOnly? CheckIn = null,
    DateOnly? CheckOut = null,
    string? Provider = null,
    string? PolicyNumber = null,
    string? CoverageScope = null,
    string? ScreeningStatus = null,
    string? FlaggedReason = null,
    DateTime? ExpiresAt = null,
    DateTime? VerifiedAt = null);
