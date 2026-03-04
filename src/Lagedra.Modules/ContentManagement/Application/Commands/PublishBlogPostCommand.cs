using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Application.Commands;

public sealed record PublishBlogPostCommand(Guid Id) : IRequest<Result>;

public sealed class PublishBlogPostCommandHandler(
    ContentDbContext dbContext)
    : IRequestHandler<PublishBlogPostCommand, Result>
{
    public async Task<Result> Handle(PublishBlogPostCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var post = await dbContext.BlogPosts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (post is null)
        {
            return Result.Failure(new Error("BlogPost.NotFound", "Blog post not found."));
        }

        post.Publish();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
