using Lagedra.Modules.JurisdictionPacks.Domain.Enums;

namespace Lagedra.Modules.JurisdictionPacks.Application.DTOs;

public sealed record PackVersionDetailDto(
    Guid VersionId,
    Guid PackId,
    int VersionNumber,
    PackVersionStatus Status,
    DateTime? EffectiveDate,
    DateTime? ApprovedAt,
    Guid? ApprovedBy,
    Guid? SecondApproverId,
    IReadOnlyList<EffectiveDateRuleDto> EffectiveDateRules,
    IReadOnlyList<FieldGatingRuleDto> FieldGatingRules,
    IReadOnlyList<EvidenceScheduleDto> EvidenceSchedules);
