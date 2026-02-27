namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

public sealed class SavedListing
{
    public Guid UserId { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid? CollectionId { get; private set; }
    public DateTime SavedAt { get; private set; }

    private SavedListing() { }

    public static SavedListing Create(Guid userId, Guid listingId, Guid? collectionId = null) =>
        new()
        {
            UserId = userId,
            ListingId = listingId,
            CollectionId = collectionId,
            SavedAt = DateTime.UtcNow
        };

    public void MoveToCollection(Guid? collectionId)
    {
        CollectionId = collectionId;
    }
}
