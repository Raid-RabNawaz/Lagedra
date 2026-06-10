using System.Diagnostics.CodeAnalysis;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

/// <summary>
/// A best-effort draft extracted from a public listing URL the host owns.
/// Every field is nullable on purpose: this is a transform-only suggestion
/// payload (URL -> DTO) that is never persisted. The frontend maps whatever is
/// present onto the existing listing form defaults; anything missing simply
/// falls back to the wizard's normal defaults. Field names intentionally mirror
/// the frontend form so mapping stays trivial.
/// </summary>
[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
    Justification = "Suggestion DTO mirroring the frontend; serialized to JSON as a string.")]
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
    Justification = "Suggestion DTO mirroring the frontend; serialized to JSON as a string.")]
public sealed record ImportedListingDraftDto(
    string? Title,
    string? Description,
    string? PropertyType,
    int? Bedrooms,
    decimal? Bathrooms,
    int? SquareFootage,
    int? MaxGuests,
    string? CheckInTime,
    string? CheckOutTime,
    long? MonthlyRentCents,
    long? NightlyRateCents,
    string? Currency,
    string? ApproxAddress,
    IReadOnlyList<string>? AmenityHints,
    IReadOnlyList<ImportedPhotoCandidateDto>? Photos,
    string? SourceUrl,
    string? SourceHost,
    bool? PetsAllowed = null,
    bool? SmokingAllowed = null,
    bool? PartiesAllowed = null,
    string? QuietHoursStart = null,
    string? QuietHoursEnd = null,
    string? HouseRules = null,
    string? CancellationPolicy = null)
{
    /// <summary>An entirely empty draft (used when a page yields nothing usable).</summary>
    public static ImportedListingDraftDto Empty(string? sourceUrl, string? sourceHost) =>
        new(
            Title: null,
            Description: null,
            PropertyType: null,
            Bedrooms: null,
            Bathrooms: null,
            SquareFootage: null,
            MaxGuests: null,
            CheckInTime: null,
            CheckOutTime: null,
            MonthlyRentCents: null,
            NightlyRateCents: null,
            Currency: null,
            ApproxAddress: null,
            AmenityHints: null,
            Photos: null,
            SourceUrl: sourceUrl,
            SourceHost: sourceHost);
}
