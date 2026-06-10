using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Application.DTOs;

public sealed record DecisionDto(
    string Summary,
    decimal? AwardAmount,
    DateTime DecidedAt,
    bool IsStructured,
    DecisionOutcome? Outcome,
    DecisionSeverity? Severity,
    IReadOnlyList<DecisionPenaltyDto> Penalties);
