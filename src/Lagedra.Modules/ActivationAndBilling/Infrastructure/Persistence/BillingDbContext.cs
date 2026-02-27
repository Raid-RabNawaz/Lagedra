using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Entities;
using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;

public sealed class BillingDbContext(
    DbContextOptions<BillingDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "activation_billing";

    public DbSet<DealApplication> DealApplications => Set<DealApplication>();
    public DbSet<BillingAccount> BillingAccounts => Set<BillingAccount>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<DealPaymentConfirmation> DealPaymentConfirmations => Set<DealPaymentConfirmation>();
    public DbSet<DamageClaim> DamageClaims => Set<DamageClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
