using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lagedra.Modules.StructuredInquiry.Domain.Entities;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Configurations;

public sealed class InquiryQuestionConfiguration : IEntityTypeConfiguration<InquiryQuestion>
{
    public void Configure(EntityTypeBuilder<InquiryQuestion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("questions");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.SessionId).IsRequired();
        builder.HasIndex(q => q.SessionId);

        builder.Property(q => q.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(q => q.PredefinedQuestionId).IsRequired();

        builder.HasOne(q => q.Answer)
            .WithOne()
            .HasForeignKey<InquiryAnswer>(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
