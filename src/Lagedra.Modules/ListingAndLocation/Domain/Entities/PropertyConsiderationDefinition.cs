using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

public sealed class PropertyConsiderationDefinition : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string IconKey { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    private PropertyConsiderationDefinition() { }

    public static PropertyConsiderationDefinition Create(string name, string iconKey, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconKey);

        return new PropertyConsiderationDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            IconKey = iconKey,
            IsActive = true,
            SortOrder = sortOrder
        };
    }

    public void Update(string name, string iconKey, bool isActive, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconKey);

        Name = name;
        IconKey = iconKey;
        IsActive = isActive;
        SortOrder = sortOrder;
    }
}
