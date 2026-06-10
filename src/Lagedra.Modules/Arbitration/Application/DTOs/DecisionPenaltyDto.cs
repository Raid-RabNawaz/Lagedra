using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Application.DTOs;

public sealed record DecisionPenaltyDto(
    Guid PenaltyId,
    Guid PartyUserId,
    PenaltyType PenaltyType,
    long? AmountCents,
    string? Description);
