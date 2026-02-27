using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.Entities;

public sealed class FieldGatingRule : Entity<Guid>
{
    public Guid VersionId { get; private set; }
    public string FieldName { get; private set; } = string.Empty;
    public GatingType GatingType { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string? Condition { get; private set; }

    private FieldGatingRule() { }

    internal static FieldGatingRule Create(Guid versionId, string fieldName, GatingType gatingType, string value, string? condition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new FieldGatingRule
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            FieldName = fieldName,
            GatingType = gatingType,
            Value = value,
            Condition = condition
        };
    }
}
