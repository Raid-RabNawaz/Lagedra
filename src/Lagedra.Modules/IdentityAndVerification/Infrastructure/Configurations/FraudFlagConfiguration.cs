using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class FraudFlagConfiguration : IEntityTypeConfiguration<FraudFlag>
{
    public void Configure(EntityTypeBuilder<FraudFlag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("fraud_flags");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId).IsRequired();
        builder.HasIndex(f => f.UserId);

        builder.Property(f => f.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(f => f.Source).HasMaxLength(200).IsRequired();
        builder.Property(f => f.RaisedAt).IsRequired();
        builder.Property(f => f.SlaDeadline).IsRequired();

        builder.Ignore(f => f.DomainEvents);
    }
}
