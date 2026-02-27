namespace Lagedra.Modules.Arbitration.Presentation.Contracts;

public sealed record IssueDecisionRequest(
    string DecisionSummary,
    decimal? AwardAmount);
