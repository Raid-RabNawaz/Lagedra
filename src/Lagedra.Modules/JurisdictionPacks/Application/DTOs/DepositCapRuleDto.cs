namespace Lagedra.Modules.JurisdictionPacks.Application.DTOs;

public sealed record DepositCapRuleDto(
    Guid Id,
    string JurisdictionCode,
    decimal MaxMultiplier,
    string? ExceptionCondition,
    decimal? ExceptionMultiplier,
    string LegalReference);
