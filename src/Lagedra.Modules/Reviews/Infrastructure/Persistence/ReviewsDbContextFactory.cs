using Lagedra.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.Reviews.Infrastructure.Persistence;

public sealed class ReviewsDbContextFactory : IDesignTimeDbContextFactory<ReviewsDbContext>
{
    public ReviewsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=lagedra;Username=lagedra;Password=lagedra";

        var optionsBuilder = new DbContextOptionsBuilder<ReviewsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ReviewsDbContext(optionsBuilder.Options, new SystemClock());
    }
}
