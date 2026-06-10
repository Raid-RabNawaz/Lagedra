using System.Diagnostics.CodeAnalysis;

namespace Lagedra.Modules.ListingAndLocation.Presentation.Contracts;

/// <summary>
/// Request body for POST /v1/listings/import-from-url. The host must confirm
/// they own the listing and have rights to its content; the server records this
/// attestation and refuses to fetch anything without it.
/// </summary>
[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
    Justification = "Raw user-supplied input that may be invalid; validated by the handler.")]
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
    Justification = "Raw user-supplied input that may be invalid; validated by the handler.")]
public sealed record ImportListingFromUrlRequest(
    string Url,
    bool HostAttestation);
