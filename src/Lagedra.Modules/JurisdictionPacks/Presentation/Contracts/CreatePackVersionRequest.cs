using Lagedra.Modules.JurisdictionPacks.Application.Commands;

namespace Lagedra.Modules.JurisdictionPacks.Presentation.Contracts;

public sealed record CreatePackVersionRequest(string JurisdictionCode);

public sealed record UpdatePackDraftRequest(
    DateTime? EffectiveDate,
    IReadOnlyList<EffectiveDateRuleInput>? EffectiveDateRules,
    IReadOnlyList<FieldGatingRuleInput>? FieldGatingRules,
    IReadOnlyList<EvidenceScheduleInput>? EvidenceSchedules);

public sealed record ApprovePackVersionRequest(Guid ApproverId);
