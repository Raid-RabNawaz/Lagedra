using Lagedra.SharedKernel.Domain;
using Lagedra.TruthSurface.Domain.Events;

namespace Lagedra.TruthSurface.Domain;

/// <summary>
/// Immutable, cryptographically signed deal snapshot. Append-only — no deletes.
///
/// Lifecycle:
///   Draft → PendingBothConfirmations → (PendingLandlord/PendingTenant) → Confirmed
///   Confirmed → Superseded (only via pack update / legal requirement)
///
/// The canonical JSON content, SHA-256 hash, and HMAC-SHA256 signature are sealed
/// at confirmation time and never modified thereafter.
/// </summary>
public sealed class TruthSnapshot : AggregateRoot<Guid>
{
    public Guid DealId { get; private set; }
    public TruthSurfaceStatus Status { get; private set; }
    public DateTime? SealedAt { get; private set; }

    public string? CanonicalContent { get; private set; }
    public string? Hash { get; private set; }
    public string? Signature { get; private set; }

    public string ProtocolVersion { get; private set; } = string.Empty;
    public string JurisdictionPackVersion { get; private set; } = string.Empty;
    public bool InquiryClosed { get; private set; }

    public bool LandlordConfirmed { get; private set; }
    public bool TenantConfirmed { get; private set; }

    public CryptographicProof? Proof { get; private set; }

    public Guid? SupersededBySnapshotId { get; private set; }

    private TruthSnapshot() { }

    public static TruthSnapshot CreateDraft(
        Guid dealId,
        string protocolVersion,
        string jurisdictionPackVersion,
        string canonicalContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocolVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdictionPackVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalContent);

        return new TruthSnapshot
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            Status = TruthSurfaceStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            ProtocolVersion = protocolVersion,
            JurisdictionPackVersion = jurisdictionPackVersion,
            CanonicalContent = canonicalContent
        };
    }

    public void SubmitForConfirmation()
    {
        if (Status != TruthSurfaceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot submit snapshot in status '{Status}'.");
        }

        Status = TruthSurfaceStatus.PendingBothConfirmations;
    }

    public void ConfirmByLandlord()
    {
        EnsurePendingConfirmation();
        LandlordConfirmed = true;
        UpdatePendingStatus();
    }

    public void ConfirmByTenant()
    {
        EnsurePendingConfirmation();
        TenantConfirmed = true;
        UpdatePendingStatus();
    }

    /// <summary>
    /// Seals the snapshot cryptographically once both parties have confirmed.
    /// After sealing, the snapshot is immutable.
    /// </summary>
    public void Seal(string hash, string signature, DateTime sealedAt)
    {
        if (!LandlordConfirmed || !TenantConfirmed)
        {
            throw new InvalidOperationException("Both parties must confirm before sealing.");
        }

        if (Status == TruthSurfaceStatus.Confirmed || Status == TruthSurfaceStatus.Superseded)
        {
            throw new InvalidOperationException($"Snapshot is already sealed (status: '{Status}').");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);

        Hash = hash;
        Signature = signature;
        SealedAt = sealedAt;
        InquiryClosed = true;
        Status = TruthSurfaceStatus.Confirmed;

        Proof = new CryptographicProof(Id, hash, signature, sealedAt);

        AddDomainEvent(new TruthSurfaceConfirmedEvent(Id, DealId, hash, signature, sealedAt));
    }

    /// <summary>
    /// Marks this snapshot as superseded by a newer one (e.g. pack update).
    /// </summary>
    public void Supersede(Guid supersedingSnapshotId, string reason)
    {
        if (Status != TruthSurfaceStatus.Confirmed)
        {
            throw new InvalidOperationException("Only a confirmed snapshot can be superseded.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = TruthSurfaceStatus.Superseded;
        SupersededBySnapshotId = supersedingSnapshotId;

        AddDomainEvent(new TruthSurfaceSupersededEvent(Id, supersedingSnapshotId, DealId, reason));
    }

    private void EnsurePendingConfirmation()
    {
        if (Status is not (TruthSurfaceStatus.PendingBothConfirmations
                        or TruthSurfaceStatus.PendingLandlordConfirmation
                        or TruthSurfaceStatus.PendingTenantConfirmation))
        {
            throw new InvalidOperationException($"Snapshot is not awaiting confirmations (status: '{Status}').");
        }
    }

    private void UpdatePendingStatus()
    {
        Status = (LandlordConfirmed, TenantConfirmed) switch
        {
            (true, true) => Status, // both done — caller will seal
            (true, false) => TruthSurfaceStatus.PendingTenantConfirmation,
            (false, true) => TruthSurfaceStatus.PendingLandlordConfirmation,
            _ => TruthSurfaceStatus.PendingBothConfirmations
        };
    }
}
