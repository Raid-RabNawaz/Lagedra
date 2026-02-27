namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

public sealed class ListingConsideration
{
    public Guid ListingId { get; private set; }
    public Guid ConsiderationDefinitionId { get; private set; }

    public PropertyConsiderationDefinition? ConsiderationDefinition { get; private set; }

    private ListingConsideration() { }

    public static ListingConsideration Create(Guid listingId, Guid considerationDefinitionId) =>
        new() { ListingId = listingId, ConsiderationDefinitionId = considerationDefinitionId };
}
