using Lagedra.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.Arbitration.Infrastructure.Persistence;

public sealed class ArbitrationDbContextFactory : IDesignTimeDbContextFactory<ArbitrationDbContext>
{
    public ArbitrationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=lagedra;Username=lagedra;Password=lagedra";

        var optionsBuilder = new DbContextOptionsBuilder<ArbitrationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ArbitrationDbContext(optionsBuilder.Options, new SystemClock());
    }
}
