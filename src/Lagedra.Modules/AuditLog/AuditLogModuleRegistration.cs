using Lagedra.Modules.AuditLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.AuditLog;

public static class AuditLogModuleRegistration
{
    public static IServiceCollection AddAuditLog(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<AuditDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AuditLogModuleRegistration).Assembly));

        return services;
    }
}
