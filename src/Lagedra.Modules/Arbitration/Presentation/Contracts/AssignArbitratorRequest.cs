namespace Lagedra.Modules.Arbitration.Presentation.Contracts;

public sealed record AssignArbitratorRequest(
    Guid ArbitratorUserId,
    int ConcurrentCaseCount);
