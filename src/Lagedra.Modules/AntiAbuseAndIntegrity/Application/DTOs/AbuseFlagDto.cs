using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.DTOs;

public sealed record AbuseFlagDto(
    Guid Id,
    Guid UserId,
    FraudFlagType FlagType,
    Severity Severity,
    DateTime FlaggedAt);
