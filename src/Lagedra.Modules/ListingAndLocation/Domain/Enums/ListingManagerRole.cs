namespace Lagedra.Modules.ListingAndLocation.Domain.Enums;

/// <summary>
/// Who the listing creator is relative to the property. Property managers must
/// name a home owner with a Lagedra account so that owner can consent on the
/// lease for stays longer than 30 days.
/// </summary>
public enum ListingManagerRole
{
    Owner,
    PropertyManager
}
