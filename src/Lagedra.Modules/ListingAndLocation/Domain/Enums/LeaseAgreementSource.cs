namespace Lagedra.Modules.ListingAndLocation.Domain.Enums;

/// <summary>
/// Which lease agreement binds a booking on this listing. Hosts can accept
/// Lagedra's counsel-vetted template for the listing's jurisdiction, supply
/// their own document (attached to the deal exactly as uploaded), or opt out
/// of a lease entirely.
/// </summary>
public enum LeaseAgreementSource
{
    LagedraTemplate,
    HostProvided,
    None
}
