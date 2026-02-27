using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetListingShareUrlQuery(Guid ListingId)
    : IRequest<Result<ListingShareUrlDto>>;

public sealed record ListingShareUrlDto(Uri ShareUrl);

public sealed class GetListingShareUrlQueryHandler(
    ListingsDbContext dbContext,
    IConfiguration configuration)
    : IRequestHandler<GetListingShareUrlQuery, Result<ListingShareUrlDto>>
{
    public async Task<Result<ListingShareUrlDto>> Handle(
        GetListingShareUrlQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exists = await dbContext.Listings
            .AsNoTracking()
            .AnyAsync(l => l.Id == request.ListingId
                && (l.Status == Domain.Enums.ListingStatus.Published || l.Status == Domain.Enums.ListingStatus.Activated),
                cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            return Result<ListingShareUrlDto>.Failure(
                new Error("Listing.NotFound", "Listing not found or not published."));
        }

        var baseUrl = configuration["App:FrontendUrl"] ?? "http://localhost:3000";
        var shareUrl = new Uri($"{baseUrl}/listings/{request.ListingId}");

        return Result<ListingShareUrlDto>.Success(new ListingShareUrlDto(shareUrl));
    }
}
