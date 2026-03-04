using Lagedra.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;

public sealed class IntegrityDbContextFactory : IDesignTimeDbContextFactory<IntegrityDbContext>
{
    public IntegrityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=lagedra;Username=lagedra;Password=lagedra";

        var optionsBuilder = new DbContextOptionsBuilder<IntegrityDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new IntegrityDbContext(optionsBuilder.Options, new SystemClock());
    }
}
