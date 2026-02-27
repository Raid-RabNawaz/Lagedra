using Lagedra.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;

public sealed class RiskDbContextFactory : IDesignTimeDbContextFactory<RiskDbContext>
{
    public RiskDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=lagedra;Username=lagedra;Password=lagedra";

        var optionsBuilder = new DbContextOptionsBuilder<RiskDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new RiskDbContext(optionsBuilder.Options, new SystemClock());
    }
}
