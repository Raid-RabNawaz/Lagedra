using System.Diagnostics.CodeAnalysis;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.Ai;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Transforms a public listing URL the host owns into a best-effort draft used
/// to pre-fill the create-listing wizard. This command is intentionally
/// read-only: it persists nothing, and on any failure the wizard still works
/// with its normal defaults.
/// </summary>
[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings",
    Justification = "Raw user-supplied input that may be invalid; validated by the handler.")]
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
    Justification = "Raw user-supplied input that may be invalid; validated by the handler.")]
public sealed record ImportListingFromUrlCommand(
    Guid RequestedByUserId,
    string Url,
    bool HostAttestation) : IRequest<Result<ImportedListingDraftDto>>;

public sealed partial class ImportListingFromUrlCommandHandler(
    IListingImportClient importClient,
    IListingMetadataExtractor extractor,
    ILogger<ImportListingFromUrlCommandHandler> logger,
    IListingDraftAiEnricher? aiEnricher = null)
    : IRequestHandler<ImportListingFromUrlCommand, Result<ImportedListingDraftDto>>
{
    private static readonly Error AttestationRequired = new(
        "Import.AttestationRequired",
        "You must confirm that this listing belongs to you and that you have rights to its content.");

    private static readonly Error InvalidUrl = new(
        "Import.InvalidUrl",
        "Enter a valid public listing URL beginning with http:// or https://.");

    private static readonly Error RobotsBlocked = new(
        "Import.RobotsBlocked",
        "The source site's robots.txt does not permit automated fetching of this page.");

    private static readonly Error FetchFailed = new(
        "Import.FetchFailed",
        "We could not read that page. Check the URL or enter the listing details manually.");

    public async Task<Result<ImportedListingDraftDto>> Handle(
        ImportListingFromUrlCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.HostAttestation)
        {
            return Result<ImportedListingDraftDto>.Failure(AttestationRequired);
        }

        if (!ListingImportPolicy.TryNormalizeUrl(request.Url, out var url) || url is null)
        {
            return Result<ImportedListingDraftDto>.Failure(InvalidUrl);
        }

        // Record the host's ownership attestation for auditability.
        LogAttestationAccepted(logger, request.RequestedByUserId, url.Host, DateTimeOffset.UtcNow);

        var robots = await importClient.FetchRobotsAsync(url, cancellationToken).ConfigureAwait(false);
        if (!ListingImportPolicy.IsPathAllowed(robots, url.AbsolutePath))
        {
            LogRobotsBlocked(logger, url.Host, url.AbsolutePath);
            return Result<ImportedListingDraftDto>.Failure(RobotsBlocked);
        }

        var fetched = await importClient.FetchAsync(url, cancellationToken).ConfigureAwait(false);
        if (fetched is null)
        {
            return Result<ImportedListingDraftDto>.Failure(FetchFailed);
        }

        var draft = extractor.Extract(fetched.Html, fetched.FinalUrl);

        // Best-effort AI enrichment fills gaps the structured extractor missed
        // (common for JS-rendered pages). It is a no-op unless configured and
        // flagged on, and never overwrites values we already extracted.
        if (aiEnricher is not null)
        {
            draft = await aiEnricher
                .EnrichAsync(draft, fetched.Html, fetched.FinalUrl, cancellationToken)
                .ConfigureAwait(false);
        }

        return Result<ImportedListingDraftDto>.Success(draft);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Listing import attestation accepted. User {UserId} attested ownership of {Host} at {AttestedAtUtc}.")]
    private static partial void LogAttestationAccepted(
        ILogger logger,
        Guid userId,
        string host,
        DateTimeOffset attestedAtUtc);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Listing import blocked by robots.txt for {Host}{Path}.")]
    private static partial void LogRobotsBlocked(ILogger logger, string host, string path);
}
