using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.Reviews.Domain.Aggregates;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Infrastructure.Persistence;

public sealed class ReviewsDbContext(
    DbContextOptions<ReviewsDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "reviews";

    public DbSet<StayReview> StayReviews => Set<StayReview>();
    public DbSet<StayReviewWindow> StayReviewWindows => Set<StayReviewWindow>();
    public DbSet<PartnerServiceReview> PartnerServiceReviews => Set<PartnerServiceReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);
    }
}
