using Lagedra.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

public sealed class InquiryDbContextFactory : IDesignTimeDbContextFactory<InquiryDbContext>
{
    public InquiryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=lagedra;Username=lagedra;Password=lagedra";

        var optionsBuilder = new DbContextOptionsBuilder<InquiryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new InquiryDbContext(optionsBuilder.Options, new SystemClock());
    }
}
