using Lagedra.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;

public sealed class InsuranceDbContextFactory : IDesignTimeDbContextFactory<InsuranceDbContext>
{
    public InsuranceDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=lagedra;Username=lagedra;Password=lagedra";

        var optionsBuilder = new DbContextOptionsBuilder<InsuranceDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new InsuranceDbContext(optionsBuilder.Options, new SystemClock());
    }
}
