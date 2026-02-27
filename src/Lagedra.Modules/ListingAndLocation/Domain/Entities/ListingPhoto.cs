using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

public sealed class ListingPhoto : Entity<Guid>
{
    public Guid ListingId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public Uri? Url { get; private set; } 
    public string? Caption { get; private set; }
    public bool IsCover { get; private set; }
    public int SortOrder { get; private set; }

    private ListingPhoto() { }

    public static ListingPhoto Create(
        Guid listingId, string storageKey, Uri url, string? caption, bool isCover, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        return new ListingPhoto
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            StorageKey = storageKey,
            Url = url,
            Caption = caption,
            IsCover = isCover,
            SortOrder = sortOrder
        };
    }

    public void SetCaption(string? caption) => Caption = caption;

    public void SetCover(bool isCover) => IsCover = isCover;

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
