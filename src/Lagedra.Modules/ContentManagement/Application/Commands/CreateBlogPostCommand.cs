using Lagedra.Modules.ContentManagement.Application.DTOs;
using Lagedra.Modules.ContentManagement.Domain.Aggregates;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ContentManagement.Application.Commands;

public sealed record CreateBlogPostCommand(
    string Slug,
    string Title,
    string Excerpt,
    string Content,
    Guid AuthorUserId,
    IReadOnlyList<string> Tags,
    string MetaTitle,
    string MetaDescription,
    Uri? OgImageUrl,
    int ReadingTimeMinutes) : IRequest<Result<BlogPostDetailDto>>;

public sealed class CreateBlogPostCommandHandler(
    ContentDbContext dbContext)
    : IRequestHandler<CreateBlogPostCommand, Result<BlogPostDetailDto>>
{
    public async Task<Result<BlogPostDetailDto>> Handle(
        CreateBlogPostCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var post = BlogPost.CreateDraft(
            request.Slug,
            request.Title,
            request.Excerpt,
            request.Content,
            request.AuthorUserId,
            request.Tags.ToArray(),
            request.MetaTitle,
            request.MetaDescription,
            request.OgImageUrl,
            request.ReadingTimeMinutes);

        dbContext.BlogPosts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<BlogPostDetailDto>.Success(MapToDetail(post));
    }

    private static BlogPostDetailDto MapToDetail(BlogPost p) =>
        new(p.Id, p.Slug, p.Title, p.Excerpt, p.Content, p.Status,
            p.PublishedAt, p.AuthorUserId, p.Tags, p.MetaTitle,
            p.MetaDescription, p.OgImageUrl, p.ReadingTimeMinutes, p.CreatedAt);
}
