using Lagedra.Modules.ContentManagement.Domain.Aggregates;
using Lagedra.Modules.ContentManagement.Domain.Enums;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Infrastructure.Repositories;

public sealed class BlogPostRepository(ContentDbContext dbContext)
{
    public async Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.BlogPosts
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await dbContext.BlogPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == BlogStatus.Published, cancellationToken)
            .ConfigureAwait(false);

    public void Add(BlogPost post) =>
        dbContext.BlogPosts.Add(post);
}
