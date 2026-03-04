using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

/// <summary>
/// Named collection for saved listings (e.g. "Downtown options", "Pet-friendly").
/// </summary>
public sealed class SavedListingCollections : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private SavedListingCollections() { }

    public static SavedListingCollections Create(Guid userId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Collection name must not exceed 100 characters.");
        }

        var now = DateTime.UtcNow;
        return new SavedListingCollections
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "Collection name must not exceed 100 characters.");
        }

        Name = name.Trim();
    }
}
