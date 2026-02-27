namespace Lagedra.Modules.Arbitration.Application.DTOs;

public sealed record DecisionDto(
    string Summary,
    decimal? AwardAmount,
    DateTime DecidedAt);
