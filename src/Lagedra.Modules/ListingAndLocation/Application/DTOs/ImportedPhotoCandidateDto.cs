using System.Diagnostics.CodeAnalysis;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

/// <summary>
/// A single photo discovered while importing a public listing page. These are
/// raw third-party URLs and are treated purely as suggestions: nothing is ever
/// persisted server-side. The host decides which candidates to import, and the
/// frontend then downloads and re-uploads each one through the existing media
/// pipeline (antivirus + EXIF strip) rather than storing the external URL.
/// </summary>
[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
    Justification = "Suggestion DTO mirroring the frontend; serialized to JSON as a string.")]
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
    Justification = "Suggestion DTO mirroring the frontend; serialized to JSON as a string.")]
public sealed record ImportedPhotoCandidateDto(
    string Url,
    string? AltText = null,
    int? Width = null,
    int? Height = null);
