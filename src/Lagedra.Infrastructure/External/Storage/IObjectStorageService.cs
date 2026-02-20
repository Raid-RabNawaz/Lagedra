namespace Lagedra.Infrastructure.External.Storage;

public interface IObjectStorageService
{
    Task<Uri> GeneratePresignedUploadUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default);
    Task<Uri> GeneratePresignedDownloadUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default);
    Task DeleteObjectAsync(string bucket, string key, CancellationToken ct = default);
    Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default);
    Task EnsureBucketExistsAsync(string bucket, CancellationToken ct = default);
}
