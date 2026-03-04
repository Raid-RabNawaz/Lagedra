using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;

public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "identity";

    public DbSet<IdentityProfile> IdentityProfiles => Set<IdentityProfile>();
    public DbSet<VerificationCase> VerificationCases => Set<VerificationCase>();
    public DbSet<BackgroundCheckReport> BackgroundCheckReports => Set<BackgroundCheckReport>();
    public DbSet<AffiliationVerification> AffiliationVerifications => Set<AffiliationVerification>();
    public DbSet<FraudFlag> FraudFlags => Set<FraudFlag>();
    public DbSet<HostPaymentDetails> HostPaymentDetails => Set<HostPaymentDetails>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
