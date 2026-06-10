using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Application.DTOs;

public sealed record CaseDto(
    Guid CaseId,
    Guid DealId,
    Guid FiledByUserId,
    Guid? LandlordUserId,
    Guid? TenantUserId,
    ArbitrationTier Tier,
    ArbitrationCategory Category,
    ArbitrationStatus Status,
    long FilingFeeCents,
    DateTime FiledAt,
    DateTime? EvidenceCompleteAt,
    DateTime? DecisionDueAt,
    int EvidenceSlotCount,
    Guid? AssignedArbitratorUserId,
    string? AssignedArbitratorEmail,
    DecisionDto? Decision,
    DecisionDto? PriorDecision,
    IReadOnlyList<EvidenceSlotDto>? EvidenceSlots);

public sealed record EvidenceSlotDto(
    Guid SlotId,
    string SlotType,
    Guid SubmittedBy,
    Guid EvidenceManifestId,
    DateTime SubmittedAt);
