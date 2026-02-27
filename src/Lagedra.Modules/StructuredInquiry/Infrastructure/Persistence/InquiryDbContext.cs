using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Domain.Entities;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

public sealed class InquiryDbContext(
    DbContextOptions<InquiryDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "inquiry";

    public DbSet<InquirySession> Sessions => Set<InquirySession>();
    public DbSet<InquiryQuestion> Questions => Set<InquiryQuestion>();
    public DbSet<InquiryAnswer> Answers => Set<InquiryAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InquiryDbContext).Assembly);
    }
}
