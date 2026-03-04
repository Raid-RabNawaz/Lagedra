namespace Lagedra.TruthSurface.Presentation.Contracts;

public sealed record CreateSnapshotRequest(
    Guid DealId,
    string ProtocolVersion,
    string JurisdictionPackVersion,
    string CanonicalContent);
