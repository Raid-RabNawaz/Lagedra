using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration.Events;
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
public sealed class TruthSnapshot : AggregateRoot<Guid>, IAppendOnly
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

    /// <summary>
    /// True once the snapshot is sealed. Sealing makes the agreement immutable;
    /// a locked snapshot stays the binding record even if a later payment fails.
    /// </summary>
    public bool IsLocked { get; private set; }
    public DateTime? LockedAt { get; private set; }

    // --- Consent audit (who agreed to the Truth Surface, when, from where) ---

    public Guid? TenantConsentUserId { get; private set; }
    public DateTime? TenantConsentAt { get; private set; }
    public string? TenantConsentIp { get; private set; }
    public string? TenantConsentUserAgent { get; private set; }
    public string? TenantConsentVersion { get; private set; }

    public Guid? HostConsentUserId { get; private set; }
    public DateTime? HostConsentAt { get; private set; }
    public string? HostConsentIp { get; private set; }
    public string? HostConsentUserAgent { get; private set; }
    public string? HostConsentVersion { get; private set; }

    public CryptographicProof? Proof { get; private set; }

    public Guid? SupersededBySnapshotId { get; private set; }

    private TruthSnapshot() { }

    public static TruthSnapshot CreateDraft(
        Guid dealId,
        string protocolVersion,
        string jurisdictionPackVersion,
        string canonicalContent)
        => CreateDraftWithId(Guid.NewGuid(), dealId, protocolVersion, jurisdictionPackVersion, canonicalContent);

    /// <summary>
    /// Create a draft with a caller-supplied id. Required when the canonical
    /// JSON itself must embed the snapshot id (so that the hashed payload can
    /// uniquely reference the row in storage).
    /// </summary>
    public static TruthSnapshot CreateDraftWithId(
        Guid snapshotId,
        Guid dealId,
        string protocolVersion,
        string jurisdictionPackVersion,
        string canonicalContent)
    {
        if (snapshotId == Guid.Empty)
        {
            throw new ArgumentException("Snapshot id must be non-empty.", nameof(snapshotId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(protocolVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdictionPackVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalContent);

        return new TruthSnapshot
        {
            Id = snapshotId,
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

        AddDomainEvent(new TruthSurfaceInitiatedEvent(Id, DealId));
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
    /// Records both parties' Truth Surface consent (tenant captured at request
    /// time, host at approval time) and marks both confirmations in one step,
    /// so the caller can immediately <see cref="Seal"/>. Used by the atomic
    /// host-approval path.
    /// </summary>
    public void RecordBothConsents(
        Guid tenantUserId,
        DateTime tenantConsentAt,
        string? tenantConsentIp,
        string? tenantConsentUserAgent,
        string tenantConsentVersion,
        Guid hostUserId,
        DateTime hostConsentAt,
        string? hostConsentIp,
        string? hostConsentUserAgent,
        string hostConsentVersion)
    {
        EnsurePendingConfirmation();

        ArgumentException.ThrowIfNullOrWhiteSpace(tenantConsentVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostConsentVersion);

        TenantConsentUserId = tenantUserId;
        TenantConsentAt = tenantConsentAt;
        TenantConsentIp = tenantConsentIp;
        TenantConsentUserAgent = tenantConsentUserAgent;
        TenantConsentVersion = tenantConsentVersion;

        HostConsentUserId = hostUserId;
        HostConsentAt = hostConsentAt;
        HostConsentIp = hostConsentIp;
        HostConsentUserAgent = hostConsentUserAgent;
        HostConsentVersion = hostConsentVersion;

        LandlordConfirmed = true;
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
        IsLocked = true;
        LockedAt = sealedAt;
        Status = TruthSurfaceStatus.Confirmed;

        Proof = new CryptographicProof(Id, hash, signature, sealedAt);

        AddDomainEvent(new TruthSurfaceConfirmedEvent(Id, DealId, hash, signature, sealedAt));
    }

    /// <summary>
    /// Voids the snapshot on a terminal cancel. The agreement is no longer in
    /// force. This only flips the status; the sealed content/proof remain intact
    /// for the audit trail (append-only). Not used for recoverable payment
    /// failures — those keep the snapshot Confirmed/Locked.
    /// </summary>
    public void Void(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status is TruthSurfaceStatus.Superseded or TruthSurfaceStatus.Voided)
        {
            throw new InvalidOperationException($"Cannot void snapshot in status '{Status}'.");
        }

        Status = TruthSurfaceStatus.Voided;
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
