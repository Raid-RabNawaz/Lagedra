namespace Lagedra.Modules.ListingAndLocation.Domain.Enums;

/// <summary>
/// Which lease agreement binds a booking on this listing. Hosts either accept
/// Lagedra's counsel-vetted template for the listing's jurisdiction, or supply
/// their own document which is attached to the deal exactly as uploaded.
/// </summary>
public enum LeaseAgreementSource
{
    LagedraTemplate,
    HostProvided
}
