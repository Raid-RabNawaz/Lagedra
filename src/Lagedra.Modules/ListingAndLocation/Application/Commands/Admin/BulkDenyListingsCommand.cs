using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

/// <summary>
/// Admin denies multiple listings currently <c>InReview</c> with one shared
/// reason. Each listing is processed independently so one failure does not
/// roll back successful denials in the same batch.
/// </summary>
public sealed record BulkDenyListingsCommand(
    IReadOnlyList<Guid> ListingIds,
    Guid AdminUserId,
    string Reason) : IRequest<Result<BulkDenyListingsResultDto>>;

public sealed record BulkDenyListingsResultDto(
    int Requested,
    int Denied,
    IReadOnlyList<BulkDenyListingFailureDto> Failures);

public sealed record BulkDenyListingFailureDto(
    Guid ListingId,
    string ErrorCode,
    string Detail);

public sealed class BulkDenyListingsCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<BulkDenyListingsCommand, Result<BulkDenyListingsResultDto>>
{
    private const int MaxBatchSize = 50;
    private const int MaxReasonLength = 2000;

    private static readonly Error ReasonRequired = new(
        "Listing.DenyReasonRequired",
        "A rejection reason is required so the landlord knows what to fix.");

    public async Task<Result<BulkDenyListingsResultDto>> Handle(
        BulkDenyListingsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<BulkDenyListingsResultDto>.Failure(ReasonRequired);
        }

        var reason = request.Reason.Trim();
        if (reason.Length > MaxReasonLength)
        {
            return Result<BulkDenyListingsResultDto>.Failure(
                new Error(
                    "Listing.DenyReasonTooLong",
                    $"Rejection reason must be {MaxReasonLength} characters or fewer."));
        }

        var ids = request.ListingIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return Result<BulkDenyListingsResultDto>.Failure(
                new Error("Listing.BulkDeny.Empty", "Select at least one listing to deny."));
        }

        if (ids.Count > MaxBatchSize)
        {
            return Result<BulkDenyListingsResultDto>.Failure(
                new Error(
                    "Listing.BulkDeny.TooMany",
                    $"You can deny at most {MaxBatchSize} listings at once."));
        }

        var listings = await dbContext.Listings
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byId = listings.ToDictionary(l => l.Id);
        var failures = new List<BulkDenyListingFailureDto>();
        var denied = 0;

        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var listing))
            {
                failures.Add(new BulkDenyListingFailureDto(
                    id, "Listing.NotFound", "Listing not found."));
                continue;
            }

            try
            {
                listing.DenyByAdmin(request.AdminUserId, reason);
                denied += 1;
            }
            catch (InvalidOperationException ex)
            {
                failures.Add(new BulkDenyListingFailureDto(
                    id, "Listing.DenyFailed", ex.Message));
            }
        }

        if (denied > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<BulkDenyListingsResultDto>.Success(
            new BulkDenyListingsResultDto(ids.Count, denied, failures));
    }
}
