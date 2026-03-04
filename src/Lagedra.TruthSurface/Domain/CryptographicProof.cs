using Lagedra.SharedKernel.Domain;

namespace Lagedra.TruthSurface.Domain;

public sealed class CryptographicProof : Entity<Guid>
{
    public Guid SnapshotId { get; private set; }
    public string Hash { get; private set; } = string.Empty;
    public string Signature { get; private set; } = string.Empty;
    public DateTime SignedAt { get; private set; }

    private CryptographicProof() { }

    public CryptographicProof(Guid snapshotId, string hash, string signature, DateTime signedAt)
        : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(signature);

        SnapshotId = snapshotId;
        Hash = hash;
        Signature = signature;
        SignedAt = signedAt;
    }
}
