using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Configurations;

public sealed class DirectReservationConfiguration : IEntityTypeConfiguration<DirectReservation>
{
    public void Configure(EntityTypeBuilder<DirectReservation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("direct_reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrganizationId).IsRequired();
        builder.Property(r => r.GuestName).HasMaxLength(500).IsRequired();
        builder.Property(r => r.GuestEmail).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ListingId).IsRequired();
        builder.Property(r => r.ReservedByUserId).IsRequired();

        builder.HasIndex(r => r.OrganizationId);
        builder.HasIndex(r => r.ListingId);

        builder.Ignore(r => r.DomainEvents);
    }
}
