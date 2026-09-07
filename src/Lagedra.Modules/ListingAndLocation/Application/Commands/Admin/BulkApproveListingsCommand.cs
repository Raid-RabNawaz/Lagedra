using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

/// <summary>
/// Admin approves multiple listings currently <c>InReview</c> in one request.
/// Each listing is processed independently so one failure does not roll back
/// successful approvals in the same batch.
/// </summary>
public sealed record BulkApproveListingsCommand(
    IReadOnlyList<Guid> ListingIds,
    Guid AdminUserId) : IRequest<Result<BulkApproveListingsResultDto>>;

public sealed record BulkApproveListingsResultDto(
    int Requested,
    int Approved,
    IReadOnlyList<BulkApproveListingFailureDto> Failures);

public sealed record BulkApproveListingFailureDto(
    Guid ListingId,
    string ErrorCode,
    string Detail);

public sealed class BulkApproveListingsCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<BulkApproveListingsCommand, Result<BulkApproveListingsResultDto>>
{
    private const int MaxBatchSize = 50;

    public async Task<Result<BulkApproveListingsResultDto>> Handle(
        BulkApproveListingsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ids = request.ListingIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return Result<BulkApproveListingsResultDto>.Failure(
                new Error("Listing.BulkApprove.Empty", "Select at least one listing to approve."));
        }

        if (ids.Count > MaxBatchSize)
        {
            return Result<BulkApproveListingsResultDto>.Failure(
                new Error(
                    "Listing.BulkApprove.TooMany",
                    $"You can approve at most {MaxBatchSize} listings at once."));
        }

        var listings = await dbContext.Listings
            .Where(l => ids.Contains(l.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byId = listings.ToDictionary(l => l.Id);
        var failures = new List<BulkApproveListingFailureDto>();
        var approved = 0;

        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var listing))
            {
                failures.Add(new BulkApproveListingFailureDto(
                    id, "Listing.NotFound", "Listing not found."));
                continue;
            }

            try
            {
                listing.ApproveByAdmin(request.AdminUserId);
                approved += 1;
            }
            catch (InvalidOperationException ex)
            {
                failures.Add(new BulkApproveListingFailureDto(
                    id, "Listing.ApproveFailed", ex.Message));
            }
        }

        if (approved > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<BulkApproveListingsResultDto>.Success(
            new BulkApproveListingsResultDto(ids.Count, approved, failures));
    }
}
