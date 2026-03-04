using Lagedra.Modules.JurisdictionPacks.Domain.Enums;

namespace Lagedra.Modules.JurisdictionPacks.Application.DTOs;

public sealed record FieldGatingRuleDto(
    Guid Id,
    string FieldName,
    GatingType GatingType,
    string Value,
    string? Condition);
