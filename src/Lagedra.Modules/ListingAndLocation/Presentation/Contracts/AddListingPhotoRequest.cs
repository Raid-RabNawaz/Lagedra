namespace Lagedra.Modules.ListingAndLocation.Presentation.Contracts;

public sealed record AddListingPhotoRequest(
    string StorageKey,
    Uri Url,
    string? Caption);
