using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.Modules.LeaseAgreements.Domain.Events;
using Lagedra.Modules.LeaseAgreements.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.LeaseAgreements.Domain.Aggregates;

public sealed class LeaseAgreementTemplate : AggregateRoot<Guid>
{
    public JurisdictionCode JurisdictionCode { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public Guid? ActiveVersionId { get; private set; }

    private readonly List<LeaseTemplateVersion> _versions = [];
    public IReadOnlyList<LeaseTemplateVersion> Versions => _versions.AsReadOnly();

    private LeaseAgreementTemplate() { }

    public static LeaseAgreementTemplate CreateDraft(string jurisdictionCode, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdictionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new LeaseAgreementTemplate
        {
            Id = Guid.NewGuid(),
            JurisdictionCode = JurisdictionCode.Create(jurisdictionCode),
            Title = title.Trim()
        };
    }

    public void Rename(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
    }

    public LeaseTemplateVersion AddVersion(string? bodyHtml = null)
    {
        var nextNumber = _versions.Count > 0
            ? _versions.Max(v => v.VersionNumber) + 1
            : 1;

        var version = LeaseTemplateVersion.Create(Id, nextNumber, bodyHtml);
        _versions.Add(version);
        return version;
    }

    public void ActivateVersion(Guid versionId)
    {
        var version = _versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"Version '{versionId}' not found on this template.");

        if (version.Status != LeaseTemplateVersionStatus.Active)
        {
            throw new InvalidOperationException(
                $"Version must be in Active status to set as live. Current status: '{version.Status}'.");
        }

        // Only one version may be Active per jurisdiction template. Deprecate
        // every other Active version — not just the previously published one —
        // so approved-but-never-published versions don't linger as Active.
        foreach (var other in _versions.Where(v =>
                     v.Id != versionId && v.Status == LeaseTemplateVersionStatus.Active))
        {
            other.Deprecate();
        }

        ActiveVersionId = versionId;
    }

    public void Publish(Guid versionId)
    {
        var version = _versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"Version '{versionId}' not found on this template.");

        if (!version.HasDualApproval)
        {
            throw new InvalidOperationException("Version requires dual-control approval before publishing.");
        }

        ActivateVersion(versionId);

        AddDomainEvent(new LeaseAgreementTemplatePublishedEvent(
            Id,
            JurisdictionCode.Code,
            versionId,
            version.VersionNumber));
    }

    /// <summary>
    /// Idempotent startup helper: ensure the given version is dual-approved,
    /// Active, and set as the live <see cref="ActiveVersionId"/>.
    /// </summary>
    public void PublishSeedVersion(Guid versionId)
    {
        var version = _versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"Version '{versionId}' not found on this template.");

        version.ApplySeedPublication();
        ActivateVersion(versionId);

        AddDomainEvent(new LeaseAgreementTemplatePublishedEvent(
            Id,
            JurisdictionCode.Code,
            versionId,
            version.VersionNumber));
    }

    public void DeprecateVersion(Guid versionId)
    {
        var version = _versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"Version '{versionId}' not found on this template.");

        version.Deprecate();

        if (ActiveVersionId == versionId)
        {
            ActiveVersionId = null;
        }
    }
}
