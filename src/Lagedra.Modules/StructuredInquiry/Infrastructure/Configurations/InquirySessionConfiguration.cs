using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Configurations;

public sealed class InquirySessionConfiguration : IEntityTypeConfiguration<InquirySession>
{
    public void Configure(EntityTypeBuilder<InquirySession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("sessions");
        builder.HasKey(s => s.Id);

        // Phase 17 — listing + tenant are the new identity. DealId is now
        // nullable because pre-booking inquiries don't have one yet.
        builder.Property(s => s.ListingId).IsRequired();
        builder.Property(s => s.TenantUserId).IsRequired();
        builder.Property(s => s.DealId).IsRequired(false);

        builder.HasIndex(s => s.DealId);
        builder.HasIndex(s => s.ListingId);
        // Used by the listing detail page to find a tenant's existing
        // pre-booking thread for "continue conversation" semantics.
        builder.HasIndex(s => new { s.ListingId, s.TenantUserId });

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasMany(s => s.Questions)
            .WithOne()
            .HasForeignKey(q => q.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
