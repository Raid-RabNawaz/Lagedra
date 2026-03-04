namespace Lagedra.Modules.Evidence.Presentation.Contracts;

public sealed record EvidenceManifestResponse(
    Guid ManifestId,
    Guid DealId,
    string ManifestType,
    string Status,
    DateTime CreatedAt,
    DateTime? SealedAt,
    string? HashOfAllFiles,
    IReadOnlyList<EvidenceUploadResponse> Uploads);

public sealed record EvidenceUploadResponse(
    Guid UploadId,
    string OriginalFileName,
    string MimeType,
    string? FileHash,
    DateTime UploadedAt);
