using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.Entities;

public sealed class EvidenceSchedule : Entity<Guid>
{
    public Guid VersionId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string MinimumRequirements { get; private set; } = string.Empty;

    private EvidenceSchedule() { }

    internal static EvidenceSchedule Create(Guid versionId, string category, string minimumRequirements)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(minimumRequirements);

        return new EvidenceSchedule
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            Category = category,
            MinimumRequirements = minimumRequirements
        };
    }
}
