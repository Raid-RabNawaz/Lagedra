using Lagedra.Modules.JurisdictionPacks.Domain.Entities;
using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Lagedra.Modules.JurisdictionPacks.Domain.Events;
using Lagedra.Modules.JurisdictionPacks.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.Aggregates;

public sealed class JurisdictionPack : AggregateRoot<Guid>
{
    public JurisdictionCode JurisdictionCode { get; private set; } = null!;
    public Guid? ActiveVersionId { get; private set; }

    private readonly List<PackVersion> _versions = [];
    public IReadOnlyList<PackVersion> Versions => _versions.AsReadOnly();

    private JurisdictionPack() { }

    public static JurisdictionPack CreateDraft(string jurisdictionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdictionCode);

        return new JurisdictionPack
        {
            Id = Guid.NewGuid(),
            JurisdictionCode = JurisdictionCode.Create(jurisdictionCode)
        };
    }

    public PackVersion AddVersion()
    {
        var nextNumber = _versions.Count > 0
            ? _versions.Max(v => v.VersionNumber) + 1
            : 1;

        var version = PackVersion.Create(Id, nextNumber);
        _versions.Add(version);
        return version;
    }

    public void ActivateVersion(Guid versionId)
    {
        var version = _versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"Version '{versionId}' not found on this pack.");

        if (version.Status != PackVersionStatus.Active)
        {
            throw new InvalidOperationException($"Version must be in Active status to set as active. Current status: '{version.Status}'.");
        }

        if (ActiveVersionId.HasValue && ActiveVersionId != versionId)
        {
            var previous = _versions.FirstOrDefault(v => v.Id == ActiveVersionId.Value);
            previous?.Deprecate();
        }

        ActiveVersionId = versionId;
    }

    public void Publish(Guid versionId)
    {
        var version = _versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"Version '{versionId}' not found on this pack.");

        if (!version.HasDualApproval)
        {
            throw new InvalidOperationException("Version requires dual-control approval before publishing.");
        }

        ActivateVersion(versionId);

        AddDomainEvent(new JurisdictionPackPublishedEvent(
            Id,
            JurisdictionCode.Code,
            versionId,
            version.VersionNumber));
    }

    public void DeprecateVersion(Guid versionId)
    {
        var version = _versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"Version '{versionId}' not found on this pack.");

        version.Deprecate();

        if (ActiveVersionId == versionId)
        {
            ActiveVersionId = null;
        }
    }
}
