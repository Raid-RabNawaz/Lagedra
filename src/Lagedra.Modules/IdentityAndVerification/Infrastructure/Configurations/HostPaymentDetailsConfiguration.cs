using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class HostPaymentDetailsConfiguration : IEntityTypeConfiguration<HostPaymentDetails>
{
    public void Configure(EntityTypeBuilder<HostPaymentDetails> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("host_payment_details");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.HostUserId).IsRequired();
        builder.HasIndex(h => h.HostUserId).IsUnique();

        builder.Property(h => h.EncryptedPaymentInfo).IsRequired();
    }
}
