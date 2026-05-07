using System.Diagnostics.CodeAnalysis;
using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Application.Queries;
using Lagedra.Auth.Domain;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Auth.Application.Commands;

/// <summary>
/// Uploads an avatar image for the signed-in user, stores it in the users
/// object-storage bucket and updates <see cref="ApplicationUser.ProfilePhotoUrl"/>.
/// </summary>
public sealed record UploadProfilePhotoCommand(
    Guid UserId,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    Func<CancellationToken, Task<Stream>> OpenReadStream)
    : IRequest<Result<UserProfileDto>>;

public sealed partial class UploadProfilePhotoCommandHandler(
    UserManager<ApplicationUser> userManager,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions,
    ILogger<UploadProfilePhotoCommandHandler> logger)
    : IRequestHandler<UploadProfilePhotoCommand, Result<UserProfileDto>>
{
    private const long MaxImageBytes = 5L * 1024 * 1024;
    private readonly string _bucket = storageOptions.Value.UsersBucket;
    private readonly ILogger<UploadProfilePhotoCommandHandler> _logger = logger;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/heic",
        "image/heif",
    };

    public async Task<Result<UserProfileDto>> Handle(
        UploadProfilePhotoCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            return Result<UserProfileDto>.Failure(
                new Error("Profile.Photo.InvalidFileName", "A file name is required."));
        }

        if (request.SizeBytes <= 0)
        {
            return Result<UserProfileDto>.Failure(
                new Error("Profile.Photo.EmptyFile", "The selected file is empty."));
        }

        var mime = string.IsNullOrWhiteSpace(request.MimeType)
            ? "application/octet-stream"
            : request.MimeType;

        if (!AllowedMimeTypes.Contains(mime))
        {
            return Result<UserProfileDto>.Failure(
                new Error("Profile.Photo.UnsupportedFileType",
                    $"File type '{mime}' is not allowed. Allowed: JPEG, PNG, GIF, WebP, HEIC."));
        }

        if (request.SizeBytes > MaxImageBytes)
        {
            return Result<UserProfileDto>.Failure(
                new Error("Profile.Photo.FileTooLarge",
                    $"Profile photos must be under {MaxImageBytes / (1024 * 1024)} MB."));
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var safeFileName = SanitizeFileName(request.OriginalFileName);
        var storageKey = $"avatars/{request.UserId}/{Guid.NewGuid()}/{safeFileName}";

        await storageService.EnsureBucketExistsAsync(_bucket, cancellationToken).ConfigureAwait(false);
        await storageService.EnsurePublicReadPolicyAsync(_bucket, cancellationToken).ConfigureAwait(false);

        var source = await request.OpenReadStream(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            await storageService
                .UploadObjectAsync(_bucket, storageKey, source, mime, cancellationToken)
                .ConfigureAwait(false);
        }

        var newUrl = storageService.GetPublicObjectUrl(_bucket, storageKey);
        var previousUrl = user.ProfilePhotoUrl;

        user.ProfilePhotoUrl = newUrl;
        var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            await TryDeleteOrphanAsync(storageKey, cancellationToken).ConfigureAwait(false);
            return AuthErrors.IdentityError(updateResult.Errors.First().Description);
        }

        if (previousUrl is not null)
        {
            await TryDeletePreviousAsync(previousUrl, cancellationToken).ConfigureAwait(false);
        }

        return Result<UserProfileDto>.Success(GetCurrentUserQueryHandler.MapToDto(user));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Best-effort cleanup; storage SDK can surface transport, auth, or AWS exceptions and we never want them to mask the user-facing failure being propagated.")]
    private async Task TryDeleteOrphanAsync(string storageKey, CancellationToken ct)
    {
        try
        {
            await storageService.DeleteObjectAsync(_bucket, storageKey, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogCleanupFailed(_logger, ex, _bucket, storageKey);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Best-effort cleanup; storage SDK can surface transport, auth, or AWS exceptions and we never want them to mask success.")]
    private async Task TryDeletePreviousAsync(Uri previousUrl, CancellationToken ct)
    {
        try
        {
            // Only attempt deletion when the previous URL points at our own
            // users bucket. URLs that came from a third-party host (e.g. an
            // OAuth provider's avatar CDN) are left alone.
            var path = previousUrl.AbsolutePath.TrimStart('/');
            var prefix = $"{_bucket}/";
            if (!path.StartsWith(prefix, StringComparison.Ordinal))
            {
                return;
            }

            var key = path[prefix.Length..];
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            await storageService.DeleteObjectAsync(_bucket, key, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogPreviousAvatarCleanupFailed(_logger, ex, previousUrl, _bucket);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var cleaned = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(cleaned.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "avatar" : safe;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to clean up orphaned avatar object {Bucket}/{Key} after user update failure.")]
    private static partial void LogCleanupFailed(ILogger logger, Exception exception, string bucket, string key);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to delete previous avatar at {PreviousUrl}; leaving the orphaned object in {Bucket}.")]
    private static partial void LogPreviousAvatarCleanupFailed(ILogger logger, Exception exception, Uri previousUrl, string bucket);
}

/// <summary>
/// Removes the user's avatar by clearing the URL on the user record. Tries to
/// delete the underlying object from storage if it lived in our users bucket.
/// </summary>
public sealed record RemoveProfilePhotoCommand(Guid UserId) : IRequest<Result<UserProfileDto>>;

public sealed partial class RemoveProfilePhotoCommandHandler(
    UserManager<ApplicationUser> userManager,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions,
    ILogger<RemoveProfilePhotoCommandHandler> logger)
    : IRequestHandler<RemoveProfilePhotoCommand, Result<UserProfileDto>>
{
    private readonly string _bucket = storageOptions.Value.UsersBucket;
    private readonly ILogger<RemoveProfilePhotoCommandHandler> _logger = logger;

    public async Task<Result<UserProfileDto>> Handle(
        RemoveProfilePhotoCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var previousUrl = user.ProfilePhotoUrl;
        if (previousUrl is null)
        {
            return Result<UserProfileDto>.Success(GetCurrentUserQueryHandler.MapToDto(user));
        }

        user.ProfilePhotoUrl = null;
        var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            return AuthErrors.IdentityError(updateResult.Errors.First().Description);
        }

        await TryDeleteAsync(previousUrl, request.UserId, cancellationToken).ConfigureAwait(false);

        return Result<UserProfileDto>.Success(GetCurrentUserQueryHandler.MapToDto(user));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Best-effort cleanup; storage SDK can surface transport, auth, or AWS exceptions and we never want them to mask the success of the DB write.")]
    private async Task TryDeleteAsync(Uri previousUrl, Guid userId, CancellationToken ct)
    {
        try
        {
            var path = previousUrl.AbsolutePath.TrimStart('/');
            var prefix = $"{_bucket}/";
            if (!path.StartsWith(prefix, StringComparison.Ordinal))
            {
                return;
            }

            var key = path[prefix.Length..];
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            await storageService.DeleteObjectAsync(_bucket, key, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogAvatarRemoveFailed(_logger, ex, userId, _bucket);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to delete avatar object for user {UserId} from {Bucket}.")]
    private static partial void LogAvatarRemoveFailed(ILogger logger, Exception exception, Guid userId, string bucket);
}
