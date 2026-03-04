using System.Collections.Immutable;
using Lagedra.Modules.ContentManagement.Application.DTOs;
using Lagedra.Modules.ContentManagement.Domain.Aggregates;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Application.Commands;

public sealed record UpdateBlogPostCommand(
    Guid Id,
    string Title,
    string Excerpt,
    string Content,
    IReadOnlyList<string> Tags,
    string MetaTitle,
    string MetaDescription,
    Uri? OgImageUrl,
    int ReadingTimeMinutes) : IRequest<Result<BlogPostDetailDto>>;

public sealed class UpdateBlogPostCommandHandler(
    ContentDbContext dbContext)
    : IRequestHandler<UpdateBlogPostCommand, Result<BlogPostDetailDto>>
{
    public async Task<Result<BlogPostDetailDto>> Handle(
        UpdateBlogPostCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var post = await dbContext.BlogPosts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (post is null)
        {
            return Result<BlogPostDetailDto>.Failure(
                new Error("BlogPost.NotFound", "Blog post not found."));
        }

        post.Update(
            request.Title,
            request.Excerpt,
            request.Content,
            request.Tags.ToArray(),
            request.MetaTitle,
            request.MetaDescription,
            request.OgImageUrl,
            request.ReadingTimeMinutes);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<BlogPostDetailDto>.Success(MapToDetail(post));
    }

    private static BlogPostDetailDto MapToDetail(BlogPost p) =>
        new(p.Id, p.Slug, p.Title, p.Excerpt, p.Content, p.Status,
            p.PublishedAt, p.AuthorUserId, p.Tags, p.MetaTitle,
            p.MetaDescription, p.OgImageUrl, p.ReadingTimeMinutes, p.CreatedAt);
}
