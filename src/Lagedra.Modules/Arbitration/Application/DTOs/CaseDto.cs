using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Application.DTOs;

public sealed record CaseDto(
    Guid CaseId,
    Guid DealId,
    Guid FiledByUserId,
    ArbitrationTier Tier,
    ArbitrationCategory Category,
    ArbitrationStatus Status,
    long FilingFeeCents,
    DateTime FiledAt,
    DateTime? EvidenceCompleteAt,
    DateTime? DecisionDueAt,
    int EvidenceSlotCount,
    DecisionDto? Decision);
