using Lagedra.Modules.InsuranceIntegration.Domain.Enums;

namespace Lagedra.Modules.InsuranceIntegration.Application.DTOs;

public sealed record InsuranceStatusDto(
    Guid PolicyRecordId,
    Guid DealId,
    Guid TenantUserId,
    InsuranceState State,
    string? Provider,
    string? PolicyNumber,
    DateTime? VerifiedAt,
    DateTime? ExpiresAt,
    string? CoverageScope);
