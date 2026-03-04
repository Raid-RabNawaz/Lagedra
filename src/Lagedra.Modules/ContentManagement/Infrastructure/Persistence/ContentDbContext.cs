using Lagedra.Modules.ContentManagement.Domain.Aggregates;
using Lagedra.Modules.ContentManagement.Domain.Entities;
using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ContentManagement.Infrastructure.Persistence;

public sealed class ContentDbContext(
    DbContextOptions<ContentDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "content";

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<SeoPage> SeoPages => Set<SeoPage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentDbContext).Assembly);
    }
}
