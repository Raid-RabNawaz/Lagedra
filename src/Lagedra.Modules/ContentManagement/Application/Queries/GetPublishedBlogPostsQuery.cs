using Lagedra.Modules.ContentManagement.Application.DTOs;
using Lagedra.Modules.ContentManagement.Domain.Enums;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Application.Queries;

public sealed record GetPublishedBlogPostsQuery(
    int Page,
    int PageSize,
    string? Tag) : IRequest<Result<IReadOnlyList<BlogPostSummaryDto>>>;

public sealed class GetPublishedBlogPostsQueryHandler(ContentDbContext dbContext)
    : IRequestHandler<GetPublishedBlogPostsQuery, Result<IReadOnlyList<BlogPostSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<BlogPostSummaryDto>>> Handle(
        GetPublishedBlogPostsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.BlogPosts
            .AsNoTracking()
            .Where(p => p.Status == BlogStatus.Published);

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            query = query.Where(p => p.Tags.Contains(request.Tag));
        }

        var posts = await query
            .OrderByDescending(p => p.PublishedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new BlogPostSummaryDto(
                p.Id, p.Slug, p.Title, p.Excerpt, p.Status,
                p.PublishedAt, p.AuthorUserId, p.Tags,
                p.OgImageUrl, p.ReadingTimeMinutes))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<BlogPostSummaryDto>>.Success(posts);
    }
}
