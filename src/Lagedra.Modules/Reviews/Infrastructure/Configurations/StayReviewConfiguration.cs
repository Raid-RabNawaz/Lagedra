using Lagedra.Modules.Reviews.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Reviews.Infrastructure.Configurations;

public sealed class StayReviewConfiguration : IEntityTypeConfiguration<StayReview>
{
    public void Configure(EntityTypeBuilder<StayReview> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("stay_reviews");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.DealId).IsRequired();
        builder.Property(r => r.ListingId).IsRequired();
        builder.Property(r => r.ReviewerUserId).IsRequired();
        builder.Property(r => r.RevieweeUserId).IsRequired();
        builder.Property(r => r.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.OverallRating).IsRequired();
        builder.Property(r => r.PublicComment).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.PrivateFeedbackToPlatform).HasMaxLength(2000);
        builder.Property(r => r.SubmittedAt).IsRequired();

        builder.HasIndex(r => new { r.DealId, r.Direction }).IsUnique();
        builder.HasIndex(r => r.RevieweeUserId);
        builder.HasIndex(r => r.ListingId);
        builder.HasIndex(r => r.Status);

        builder.Ignore(r => r.DomainEvents);
    }
}
