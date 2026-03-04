using Lagedra.Modules.ContentManagement.Domain.Entities;
using Lagedra.Modules.ContentManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Infrastructure.Repositories;

public sealed class SeoPageRepository(ContentDbContext dbContext)
{
    public async Task<SeoPage?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await dbContext.SeoPages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken)
            .ConfigureAwait(false);

    public void Add(SeoPage page) =>
        dbContext.SeoPages.Add(page);
}
