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

        builder.Property(s => s.DealId).IsRequired();
        builder.HasIndex(s => s.DealId);

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
