using Lagedra.Modules.ContentManagement.Application.DTOs;
using Lagedra.Modules.ContentManagement.Domain.Aggregates;
using Lagedra.Modules.ContentManagement.Domain.Enums;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Application.Queries;

public sealed record GetBlogPostBySlugQuery(string Slug) : IRequest<Result<BlogPostDetailDto>>;

public sealed class GetBlogPostBySlugQueryHandler(ContentDbContext dbContext)
    : IRequestHandler<GetBlogPostBySlugQuery, Result<BlogPostDetailDto>>
{
    public async Task<Result<BlogPostDetailDto>> Handle(
        GetBlogPostBySlugQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var post = await dbContext.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Slug == request.Slug && p.Status == BlogStatus.Published,
                cancellationToken)
            .ConfigureAwait(false);

        if (post is null)
        {
            return Result<BlogPostDetailDto>.Failure(
                new Error("BlogPost.NotFound", "Blog post not found."));
        }

        return Result<BlogPostDetailDto>.Success(MapToDetail(post));
    }

    private static BlogPostDetailDto MapToDetail(BlogPost p) =>
        new(p.Id, p.Slug, p.Title, p.Excerpt, p.Content, p.Status,
            p.PublishedAt, p.AuthorUserId, p.Tags, p.MetaTitle,
            p.MetaDescription, p.OgImageUrl, p.ReadingTimeMinutes, p.CreatedAt);
}
