using Lagedra.Modules.ActivationAndBilling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.BillingAccountId).IsRequired();
        builder.HasIndex(i => i.BillingAccountId);

        builder.Property(i => i.StripeInvoiceId).HasMaxLength(255);
        builder.HasIndex(i => i.StripeInvoiceId)
            .HasFilter("\"StripeInvoiceId\" IS NOT NULL")
            .IsUnique();

        builder.Property(i => i.PeriodStart).IsRequired();
        builder.Property(i => i.PeriodEnd).IsRequired();
        builder.Property(i => i.AmountCents).IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Ignore(i => i.DomainEvents);
    }
}
