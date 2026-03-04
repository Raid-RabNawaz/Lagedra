namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

public sealed class ListingAmenity
{
    public Guid ListingId { get; private set; }
    public Guid AmenityDefinitionId { get; private set; }

    public AmenityDefinition? AmenityDefinition { get; private set; }

    private ListingAmenity() { }

    public static ListingAmenity Create(Guid listingId, Guid amenityDefinitionId) =>
        new() { ListingId = listingId, AmenityDefinitionId = amenityDefinitionId };
}
