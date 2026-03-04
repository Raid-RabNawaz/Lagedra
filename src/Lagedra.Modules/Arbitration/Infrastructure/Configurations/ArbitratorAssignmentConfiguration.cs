using Lagedra.Modules.Arbitration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Arbitration.Infrastructure.Configurations;

public sealed class ArbitratorAssignmentConfiguration : IEntityTypeConfiguration<ArbitratorAssignment>
{
    public void Configure(EntityTypeBuilder<ArbitratorAssignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("arbitrator_assignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CaseId).IsRequired();
        builder.Property(a => a.ArbitratorUserId).IsRequired();
        builder.HasIndex(a => a.ArbitratorUserId);
        builder.Property(a => a.AssignedAt).IsRequired();
        builder.Property(a => a.ConcurrentCaseCount).IsRequired();
    }
}
