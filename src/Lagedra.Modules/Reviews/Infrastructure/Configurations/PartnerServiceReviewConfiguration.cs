using Lagedra.Modules.Reviews.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Reviews.Infrastructure.Configurations;

public sealed class PartnerServiceReviewConfiguration : IEntityTypeConfiguration<PartnerServiceReview>
{
    public void Configure(EntityTypeBuilder<PartnerServiceReview> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("partner_service_reviews");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrganizationId).IsRequired();
        builder.Property(r => r.EndorsementId).IsRequired();
        builder.Property(r => r.ReviewerUserId).IsRequired();
        builder.Property(r => r.OverallRating).IsRequired();
        builder.Property(r => r.Responsiveness).IsRequired();
        builder.Property(r => r.Reliability).IsRequired();
        builder.Property(r => r.SupportQuality).IsRequired();
        builder.Property(r => r.PublicComment).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.SubmittedAt).IsRequired();

        builder.HasIndex(r => new { r.OrganizationId, r.ReviewerUserId }).IsUnique();
        builder.HasIndex(r => r.OrganizationId);

        builder.Ignore(r => r.DomainEvents);
    }
}
