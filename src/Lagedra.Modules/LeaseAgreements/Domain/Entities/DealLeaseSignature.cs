using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.LeaseAgreements.Domain.Entities;

/// <summary>
/// One party's in-app signature on a deal's Lagedra lease PDF. The document
/// hash is the PDF they signed, captured before the stamp is applied.
/// </summary>
public sealed class DealLeaseSignature : Entity<Guid>
{
    public Guid DealId { get; private set; }

    public DealLeaseSignerRole Role { get; private set; }

    public Guid SignerUserId { get; private set; }

    public string TypedName { get; private set; } = string.Empty;

    public DateTime SignedAtUtc { get; private set; }

    public string DocumentHash { get; private set; } = string.Empty;

    private DealLeaseSignature() { }

    public static DealLeaseSignature Create(
        Guid dealId,
        DealLeaseSignerRole role,
        Guid signerUserId,
        string typedName,
        string documentHash,
        DateTime signedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typedName);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentHash);

        var name = typedName.Trim();
        if (name.Length < 2)
        {
            throw new ArgumentException("Typed name must be at least 2 characters.", nameof(typedName));
        }

        return new DealLeaseSignature
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            Role = role,
            SignerUserId = signerUserId,
            TypedName = name.Length > 200 ? name[..200] : name,
            DocumentHash = documentHash.Trim(),
            SignedAtUtc = signedAtUtc,
        };
    }
}
