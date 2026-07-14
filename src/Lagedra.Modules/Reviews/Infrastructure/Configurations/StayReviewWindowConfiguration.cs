using Lagedra.Modules.Reviews.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Reviews.Infrastructure.Configurations;

public sealed class StayReviewWindowConfiguration : IEntityTypeConfiguration<StayReviewWindow>
{
    public void Configure(EntityTypeBuilder<StayReviewWindow> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("stay_review_windows");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.DealId).IsRequired();
        builder.HasIndex(w => w.DealId).IsUnique();
        builder.Property(w => w.ListingId).IsRequired();
        builder.Property(w => w.LandlordUserId).IsRequired();
        builder.Property(w => w.TenantUserId).IsRequired();
        builder.Property(w => w.OpensAt).IsRequired();
        builder.Property(w => w.ClosesAt).IsRequired();
        builder.Property(w => w.GuestSubmitted).IsRequired();
        builder.Property(w => w.HostSubmitted).IsRequired();
        builder.Property(w => w.IsPublished).IsRequired();

        builder.HasIndex(w => w.ClosesAt);
        builder.HasIndex(w => w.IsPublished);

        builder.Ignore(w => w.DomainEvents);
    }
}
