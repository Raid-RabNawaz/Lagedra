using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Storage;

public sealed partial class MinioStorageService : IObjectStorageService, IAsyncDisposable
{
    private readonly AmazonS3Client _client;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IOptions<MinioSettings> settings, ILogger<MinioStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.UseHttps
                ? $"https://{_settings.Endpoint}"
                : $"http://{_settings.Endpoint}",
            ForcePathStyle = true
        };

        _client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
    }

    public async Task<Uri> GeneratePresignedUploadUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default)
    {
        _ = ct;
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        var url = await _client.GetPreSignedURLAsync(request).ConfigureAwait(false);
        LogPresignedUpload(logger: _logger, bucket: bucket, key: key);
        return new Uri(url);
    }

    public async Task<Uri> GeneratePresignedDownloadUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default)
    {
        _ = ct;
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        var url = await _client.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return new Uri(url);
    }

    public async Task DeleteObjectAsync(string bucket, string key, CancellationToken ct = default)
    {
        await _client.DeleteObjectAsync(bucket, key, ct).ConfigureAwait(false);
        LogObjectDeleted(_logger, bucket, key);
    }

    public async Task<bool> ObjectExistsAsync(string bucket, string key, CancellationToken ct = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(bucket, key, ct).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task EnsureBucketExistsAsync(string bucket, CancellationToken ct = default)
    {
        var buckets = await _client.ListBucketsAsync(ct).ConfigureAwait(false);
        if (buckets.Buckets.Exists(b => b.BucketName == bucket))
        {
            return;
        }

        await _client.PutBucketAsync(bucket, ct).ConfigureAwait(false);
        LogBucketCreated(_logger, bucket);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Presigned upload URL generated for {Bucket}/{Key}")]
    private static partial void LogPresignedUpload(ILogger logger, string bucket, string key);

    [LoggerMessage(Level = LogLevel.Information, Message = "Object deleted from {Bucket}/{Key}")]
    private static partial void LogObjectDeleted(ILogger logger, string bucket, string key);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bucket '{Bucket}' created in MinIO")]
    private static partial void LogBucketCreated(ILogger logger, string bucket);
}
