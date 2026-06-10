namespace Lagedra.Modules.Arbitration.Presentation.Contracts;

public sealed record IssueDecisionRequest(
    string DecisionSummary,
    decimal? AwardAmount,
    bool IsStructured,
    string? Outcome,
    string? Severity,
    IReadOnlyList<DecisionPenaltyRequest>? Penalties);

public sealed record DecisionPenaltyRequest(
    Guid PartyUserId,
    string PenaltyType,
    long? AmountCents,
    string? Description);
