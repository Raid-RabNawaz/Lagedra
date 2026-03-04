namespace Lagedra.Modules.ComplianceMonitoring.Presentation.Contracts;

public sealed record ComplianceStatusResponse(
    Guid DealId,
    int OpenViolations,
    int CuredViolations,
    int EscalatedViolations,
    int TotalSignals,
    int UnprocessedSignals,
    bool IsCompliant);
