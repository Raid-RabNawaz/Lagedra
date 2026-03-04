using Lagedra.Modules.ContentManagement.Application.DTOs;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Application.Queries;

public sealed record GetAllBlogPostsAdminQuery(int Page, int PageSize)
    : IRequest<Result<IReadOnlyList<BlogPostSummaryDto>>>;

public sealed class GetAllBlogPostsAdminQueryHandler(ContentDbContext dbContext)
    : IRequestHandler<GetAllBlogPostsAdminQuery, Result<IReadOnlyList<BlogPostSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<BlogPostSummaryDto>>> Handle(
        GetAllBlogPostsAdminQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var posts = await dbContext.BlogPosts
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
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
