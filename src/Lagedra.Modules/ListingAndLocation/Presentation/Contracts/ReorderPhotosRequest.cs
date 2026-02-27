namespace Lagedra.Modules.ListingAndLocation.Presentation.Contracts;

public sealed record ReorderPhotosRequest(
    IReadOnlyList<Guid> PhotoIdsInOrder);
