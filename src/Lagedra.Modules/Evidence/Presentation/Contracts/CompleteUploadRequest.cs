namespace Lagedra.Modules.Evidence.Presentation.Contracts;

public sealed record CompleteUploadRequest(
    Guid ManifestId,
    string OriginalFileName,
    string StorageKey,
    string MimeType,
    string FileHashHex);
