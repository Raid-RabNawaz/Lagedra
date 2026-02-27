namespace Lagedra.Modules.JurisdictionPacks.Application.DTOs;

public sealed record EffectiveDateRuleDto(
    Guid Id,
    string FieldName,
    DateTime EffectiveDate);
