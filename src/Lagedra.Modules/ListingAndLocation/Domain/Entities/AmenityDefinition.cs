using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

public sealed class AmenityDefinition : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public AmenityCategory Category { get; private set; }
    public string IconKey { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    private AmenityDefinition() { }

    public static AmenityDefinition Create(string name, AmenityCategory category, string iconKey, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconKey);

        return new AmenityDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            IconKey = iconKey,
            IsActive = true,
            SortOrder = sortOrder
        };
    }

    public void Update(string name, AmenityCategory category, string iconKey, bool isActive, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconKey);

        Name = name;
        Category = category;
        IconKey = iconKey;
        IsActive = isActive;
        SortOrder = sortOrder;
    }
}
