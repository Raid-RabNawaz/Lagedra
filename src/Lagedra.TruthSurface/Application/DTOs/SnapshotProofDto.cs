namespace Lagedra.TruthSurface.Application.DTOs;

public sealed record SnapshotProofDto(
    Guid ProofId,
    string Hash,
    string Signature,
    DateTime SignedAt,
    bool IsValid);
