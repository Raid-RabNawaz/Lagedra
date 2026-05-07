namespace Lagedra.Infrastructure.External.Storage;

public interface IObjectStorageService
{
    Task<Uri> GeneratePresignedUploadUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default);
    Task<Uri> GeneratePresignedDownloadUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default);
    Task<Stream> GetObjectStreamAsync(string bucket, string key, CancellationToken ct = default);
    Task MoveObjectAsync(string sourceBucket, string sourceKey, string destBucket, string destKey, CancellationToken ct = default);
    Task DeleteObjectAsync(string bucket, string key, CancellationToken ct = default);
    Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default);
    Task EnsureBucketExistsAsync(string bucket, CancellationToken ct = default);

    /// <summary>
    /// Uploads object bytes to the bucket using a server-side PUT. Use this
    /// when proxying uploads through the API (e.g. for browser clients that
    /// cannot reach the bucket directly due to CORS).
    /// </summary>
    Task UploadObjectAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Builds the canonical public URL for an object. If the bucket is not
    /// publicly readable, callers should use a presigned download URL instead.
    /// </summary>
    Uri GetPublicObjectUrl(string bucket, string key);

    /// <summary>
    /// Ensures the bucket has a public-read policy so browsers can fetch the
    /// objects directly (used for listing media). Idempotent.
    /// </summary>
    Task EnsurePublicReadPolicyAsync(string bucket, CancellationToken ct = default);
}
