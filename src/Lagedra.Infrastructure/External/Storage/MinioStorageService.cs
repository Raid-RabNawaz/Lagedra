using Amazon;
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

        if (_settings.UseIamRole)
        {
            var region = RegionEndpoint.GetBySystemName(
                _settings.Endpoint
                    .Replace("s3.", "", StringComparison.Ordinal)
                    .Replace(".amazonaws.com", "", StringComparison.Ordinal));
            _client = new AmazonS3Client(region);
        }
        else
        {
            var config = new AmazonS3Config
            {
                ServiceURL = _settings.UseHttps
                    ? $"https://{_settings.Endpoint}"
                    : $"http://{_settings.Endpoint}",
                ForcePathStyle = true
            };
            _client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
        }
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

    public async Task<Stream> GetObjectStreamAsync(string bucket, string key, CancellationToken ct = default)
    {
        var response = await _client.GetObjectAsync(bucket, key, ct).ConfigureAwait(false);
        return response.ResponseStream;
    }

    public async Task MoveObjectAsync(string sourceBucket, string sourceKey, string destBucket, string destKey, CancellationToken ct = default)
    {
        await _client.CopyObjectAsync(new Amazon.S3.Model.CopyObjectRequest
        {
            SourceBucket = sourceBucket,
            SourceKey = sourceKey,
            DestinationBucket = destBucket,
            DestinationKey = destKey
        }, ct).ConfigureAwait(false);

        await _client.DeleteObjectAsync(sourceBucket, sourceKey, ct).ConfigureAwait(false);
        LogObjectMoved(_logger, sourceBucket, sourceKey, destBucket, destKey);
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

    public async Task UploadObjectAsync(
        string bucket,
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            AutoCloseStream = false,
            // The AWS SDK requires HTTPS when payload signing is disabled
            // (signing is what guarantees body integrity in transit). Only
            // skip signing when we're talking to the bucket over TLS.
            DisablePayloadSigning = _settings.UseHttps,
        };

        await _client.PutObjectAsync(request, ct).ConfigureAwait(false);
        LogObjectUploaded(_logger, bucket, key);
    }

    public Uri GetPublicObjectUrl(string bucket, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var encodedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));

        if (!string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
        {
            var trimmed = _settings.PublicBaseUrl.TrimEnd('/');
            return new Uri($"{trimmed}/{bucket}/{encodedKey}");
        }

        var scheme = _settings.UseHttps ? "https" : "http";
        return new Uri($"{scheme}://{_settings.Endpoint}/{bucket}/{encodedKey}");
    }

    public async Task EnsurePublicReadPolicyAsync(string bucket, CancellationToken ct = default)
    {
        var policy = $$"""
        {
          "Version": "2012-10-17",
          "Statement": [
            {
              "Effect": "Allow",
              "Principal": "*",
              "Action": ["s3:GetObject"],
              "Resource": ["arn:aws:s3:::{{bucket}}/*"]
            }
          ]
        }
        """;

        try
        {
            await _client.PutBucketPolicyAsync(new PutBucketPolicyRequest
            {
                BucketName = bucket,
                Policy = policy,
            }, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex)
        {
            // Some managed S3 deployments (e.g. with bucket-level public access
            // blocks) reject this. We swallow so callers can still proceed using
            // presigned download URLs as a fallback.
            LogPublicPolicyFailed(_logger, bucket, ex.Message);
        }
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Object moved from {SourceBucket}/{SourceKey} to {DestBucket}/{DestKey}")]
    private static partial void LogObjectMoved(ILogger logger, string sourceBucket, string sourceKey, string destBucket, string destKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bucket '{Bucket}' created in MinIO")]
    private static partial void LogBucketCreated(ILogger logger, string bucket);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Object uploaded to {Bucket}/{Key}")]
    private static partial void LogObjectUploaded(ILogger logger, string bucket, string key);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to set public-read policy on bucket '{Bucket}': {Reason}")]
    private static partial void LogPublicPolicyFailed(ILogger logger, string bucket, string reason);
}
