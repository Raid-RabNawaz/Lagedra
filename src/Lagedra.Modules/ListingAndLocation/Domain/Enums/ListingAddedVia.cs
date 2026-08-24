namespace Lagedra.Modules.ListingAndLocation.Domain.Enums;

/// <summary>
/// How a listing first entered Lagedra. Set at create time and not edited
/// afterward so analytics can attribute inventory without relying on
/// later host edits.
/// </summary>
public enum ListingAddedVia
{
    Manual,
    Url,
    Excel,
    Xml,
    Channel
}
