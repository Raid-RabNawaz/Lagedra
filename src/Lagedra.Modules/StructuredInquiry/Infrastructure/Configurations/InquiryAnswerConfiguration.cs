using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lagedra.Modules.StructuredInquiry.Domain.Entities;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Configurations;

public sealed class InquiryAnswerConfiguration : IEntityTypeConfiguration<InquiryAnswer>
{
    public void Configure(EntityTypeBuilder<InquiryAnswer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("answers");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.QuestionId).IsRequired();
        builder.HasIndex(a => a.QuestionId).IsUnique();

        builder.Property(a => a.ResponseType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.AnswerValue)
            .HasMaxLength(2000)
            .IsRequired();
    }
}
