namespace Lagedra.TruthSurface.Presentation.Contracts;

public sealed record ReconfirmSnapshotRequest(
    string NewJurisdictionPackVersion,
    string UpdatedCanonicalContent,
    string Reason);
