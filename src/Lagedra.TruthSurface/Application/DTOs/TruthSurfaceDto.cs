using Lagedra.TruthSurface.Domain;

namespace Lagedra.TruthSurface.Application.DTOs;

public sealed record TruthSurfaceDto(
    Guid SnapshotId,
    Guid DealId,
    TruthSurfaceStatus Status,
    string ProtocolVersion,
    string JurisdictionPackVersion,
    bool InquiryClosed,
    bool LandlordConfirmed,
    bool TenantConfirmed,
    DateTime CreatedAt,
    DateTime? SealedAt,
    SnapshotProofDto? Proof);
