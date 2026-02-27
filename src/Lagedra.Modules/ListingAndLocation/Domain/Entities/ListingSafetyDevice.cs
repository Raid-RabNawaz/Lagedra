namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

public sealed class ListingSafetyDevice
{
    public Guid ListingId { get; private set; }
    public Guid SafetyDeviceDefinitionId { get; private set; }

    public SafetyDeviceDefinition? SafetyDeviceDefinition { get; private set; }

    private ListingSafetyDevice() { }

    public static ListingSafetyDevice Create(Guid listingId, Guid safetyDeviceDefinitionId) =>
        new() { ListingId = listingId, SafetyDeviceDefinitionId = safetyDeviceDefinitionId };
}
