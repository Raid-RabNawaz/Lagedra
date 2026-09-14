using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class ScreeningOrderConfiguration : IEntityTypeConfiguration<ScreeningOrder>
{
    public void Configure(EntityTypeBuilder<ScreeningOrder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("screening_orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.ApplicationId).IsRequired();
        builder.HasIndex(o => o.ApplicationId);

        builder.Property(o => o.HostUserId).IsRequired();
        builder.Property(o => o.TenantUserId).IsRequired();
        builder.Property(o => o.ListingId).IsRequired();

        builder.Property(o => o.Provider)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(o => o.ExternalOrderId).HasMaxLength(80);
        builder.Property(o => o.ExternalReportId).HasMaxLength(80);
        builder.Property(o => o.ApplicationUrl).HasMaxLength(500);
        builder.Property(o => o.StripePaymentIntentId).HasMaxLength(64);
        builder.Property(o => o.LastFailureReason).HasMaxLength(500);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(o => o.ExternalOrderId);
        builder.HasIndex(o => o.StripePaymentIntentId);
    }
}
