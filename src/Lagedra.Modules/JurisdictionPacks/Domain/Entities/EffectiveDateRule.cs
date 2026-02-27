using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.Entities;

public sealed class EffectiveDateRule : Entity<Guid>
{
    public Guid VersionId { get; private set; }
    public string FieldName { get; private set; } = string.Empty;
    public DateTime EffectiveDate { get; private set; }

    private EffectiveDateRule() { }

    internal static EffectiveDateRule Create(Guid versionId, string fieldName, DateTime effectiveDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        return new EffectiveDateRule
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            FieldName = fieldName,
            EffectiveDate = effectiveDate
        };
    }
}
