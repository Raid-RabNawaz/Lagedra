namespace Lagedra.Modules.ComplianceMonitoring.Presentation.Contracts;

public sealed record RecordSignalRequest(
    string SignalType,
    string Source);
