namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record ListingPhotoDto(
    Guid Id,
    Uri? Url,
    string? Caption,
    bool IsCover,
    int SortOrder);
