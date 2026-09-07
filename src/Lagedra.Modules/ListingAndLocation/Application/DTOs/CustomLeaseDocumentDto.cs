namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

/// <summary>
/// Metadata for a host-uploaded lease agreement. Deliberately carries no
/// storage key or URL: reads go through an authorized endpoint that issues a
/// short-lived link, so the object location is never handed to a client.
/// </summary>
public sealed record CustomLeaseDocumentDto(
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc);
