using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lagedra.Modules.StructuredInquiry.Domain.Entities;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Configurations;

public sealed class InquiryOfferConfiguration : IEntityTypeConfiguration<InquiryOffer>
{
    public void Configure(EntityTypeBuilder<InquiryOffer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("offers");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.SessionId).IsRequired();
        builder.HasIndex(o => o.SessionId);
        builder.HasIndex(o => new { o.SessionId, o.Status });

        builder.Property(o => o.ProposedByUserId).IsRequired();
        builder.Property(o => o.ProposedByRole)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.RentCents).IsRequired();
        builder.Property(o => o.DepositCents).IsRequired();
        builder.Property(o => o.Note).HasMaxLength(InquiryOffer.NoteMaxLength);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.ProposedAt).IsRequired();
        builder.Property(o => o.RespondedAt);
        builder.Property(o => o.SupersedesOfferId);
    }
}
