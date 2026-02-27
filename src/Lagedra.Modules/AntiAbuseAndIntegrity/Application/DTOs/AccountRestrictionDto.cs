using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.DTOs;

public sealed record AccountRestrictionDto(
    Guid Id,
    Guid UserId,
    RestrictionLevel RestrictionLevel,
    DateTime AppliedAt,
    string Reason);
