namespace Lagedra.Modules.Evidence.Presentation.Contracts;

public sealed record RequestUploadUrlRequest(
    Guid ManifestId,
    string FileName,
    string MimeType);
