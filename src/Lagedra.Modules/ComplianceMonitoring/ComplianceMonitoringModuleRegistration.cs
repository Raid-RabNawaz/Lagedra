using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Jobs;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Repositories;
using Lagedra.Infrastructure.Eventing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.ComplianceMonitoring;

public static class ComplianceMonitoringModuleRegistration
{
    public static IServiceCollection AddComplianceMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ComplianceMonitoringDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ComplianceMonitoringDbContext>();

        services.AddScoped<MonitoredViolationRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ComplianceMonitoringModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("ComplianceScanner");
            q.AddJob<ComplianceScannerJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("ComplianceScanner-trigger")
                .WithCronSchedule("0 0 */6 * * ?")); // Every 6 hours
        });

        return services;
    }
}
