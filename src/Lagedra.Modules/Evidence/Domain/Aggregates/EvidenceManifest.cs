using System.Security.Cryptography;
using System.Text;
using Lagedra.Modules.Evidence.Domain.Entities;
using Lagedra.Modules.Evidence.Domain.Enums;
using Lagedra.Modules.Evidence.Domain.Events;
using Lagedra.Modules.Evidence.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Evidence.Domain.Aggregates;

public sealed class EvidenceManifest : AggregateRoot<Guid>
{
    private readonly List<EvidenceUpload> _uploads = [];

    public Guid DealId { get; private set; }
    public ManifestType ManifestType { get; private set; }
    public ManifestStatus Status { get; private set; }
    public DateTime? SealedAt { get; private set; }
    public string? HashOfAllFiles { get; private set; }

    public IReadOnlyList<EvidenceUpload> Uploads => _uploads.AsReadOnly();

    private EvidenceManifest() { }

    public static EvidenceManifest Create(Guid dealId, ManifestType manifestType)
    {
        var manifest = new EvidenceManifest
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            ManifestType = manifestType,
            Status = ManifestStatus.Open
        };

        manifest.AddDomainEvent(new EvidenceManifestCreatedEvent(
            manifest.Id, dealId, manifestType.ToString()));

        return manifest;
    }

    public EvidenceUpload AddUpload(string originalFileName, string storageKey, string mimeType)
    {
        if (Status != ManifestStatus.Open)
        {
            throw new InvalidOperationException("Cannot add uploads to a sealed manifest.");
        }

        var upload = EvidenceUpload.Create(Id, originalFileName, storageKey, mimeType);
        _uploads.Add(upload);

        AddDomainEvent(new EvidenceUploadedEvent(
            upload.Id, Id, originalFileName, string.Empty));

        return upload;
    }

    public void Seal()
    {
        if (Status != ManifestStatus.Open)
        {
            throw new InvalidOperationException("Manifest is already sealed.");
        }

        if (_uploads.Count == 0)
        {
            throw new InvalidOperationException("Cannot seal a manifest with no uploads.");
        }

        HashOfAllFiles = ComputeCompositeHash();
        SealedAt = DateTime.UtcNow;
        Status = ManifestStatus.Sealed;

        AddDomainEvent(new EvidenceManifestSealedEvent(
            Id, DealId, HashOfAllFiles, SealedAt.Value));
    }

    private string ComputeCompositeHash()
    {
        var orderedHashes = _uploads
            .Where(u => u.FileHash is not null)
            .OrderBy(u => u.Id)
            .Select(u => u.FileHash!.Value);

        var combined = string.Join("|", orderedHashes);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexStringLower(bytes);
    }
}
