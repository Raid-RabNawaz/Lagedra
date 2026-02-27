using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.Compliance.Infrastructure.Repositories;
using Lagedra.Compliance.Infrastructure.Services;
using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Compliance;

public static class ComplianceModuleRegistration
{
    public static IServiceCollection AddCompliance(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ComplianceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ComplianceDbContext>();

        services.AddScoped<ViolationRepository>();
        services.AddScoped<TrustLedgerRepository>();
        services.AddScoped<IUserViolationCountProvider, UserViolationCountProvider>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ComplianceModuleRegistration).Assembly));

        return services;
    }
}
