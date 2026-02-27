using Lagedra.Modules.ContentManagement.Application.DTOs;
using Lagedra.Modules.ContentManagement.Domain.Enums;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Application.Queries;

public sealed record GetSitemapEntriesQuery : IRequest<Result<IReadOnlyList<SitemapEntryDto>>>;

public sealed class GetSitemapEntriesQueryHandler(ContentDbContext dbContext)
    : IRequestHandler<GetSitemapEntriesQuery, Result<IReadOnlyList<SitemapEntryDto>>>
{
    public async Task<Result<IReadOnlyList<SitemapEntryDto>>> Handle(
        GetSitemapEntriesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blogEntries = await dbContext.BlogPosts
            .AsNoTracking()
            .Where(p => p.Status == BlogStatus.Published)
            .Select(p => new SitemapEntryDto(
                $"/blog/{p.Slug}",
                p.PublishedAt ?? p.CreatedAt,
                "weekly",
                0.7))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pageEntries = await dbContext.SeoPages
            .AsNoTracking()
            .Where(p => !p.NoIndex)
            .Select(p => new SitemapEntryDto(
                $"/pages/{p.Slug}",
                p.UpdatedAtUtc,
                "monthly",
                0.5))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = blogEntries.Concat(pageEntries).ToList();

        return Result<IReadOnlyList<SitemapEntryDto>>.Success(entries);
    }
}
