using Lagedra.Modules.JurisdictionPacks.Domain.Enums;

namespace Lagedra.Modules.JurisdictionPacks.Presentation.Contracts;

public sealed record JurisdictionPackResponse(
    Guid PackId,
    string JurisdictionCode,
    Guid? ActiveVersionId,
    IReadOnlyList<PackVersionSummaryResponse> Versions);

public sealed record PackVersionSummaryResponse(
    Guid VersionId,
    int VersionNumber,
    PackVersionStatus Status,
    DateTime? EffectiveDate,
    DateTime? ApprovedAt,
    Guid? ApprovedBy,
    Guid? SecondApproverId);

public sealed record PackVersionDetailResponse(
    Guid VersionId,
    Guid PackId,
    int VersionNumber,
    PackVersionStatus Status,
    DateTime? EffectiveDate,
    DateTime? ApprovedAt,
    Guid? ApprovedBy,
    Guid? SecondApproverId,
    IReadOnlyList<EffectiveDateRuleResponse> EffectiveDateRules,
    IReadOnlyList<FieldGatingRuleResponse> FieldGatingRules,
    IReadOnlyList<EvidenceScheduleResponse> EvidenceSchedules);

public sealed record EffectiveDateRuleResponse(Guid Id, string FieldName, DateTime EffectiveDate);
public sealed record FieldGatingRuleResponse(Guid Id, string FieldName, GatingType GatingType, string Value, string? Condition);
public sealed record EvidenceScheduleResponse(Guid Id, string Category, string MinimumRequirements);
