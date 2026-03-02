using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Evidence.Infrastructure.Jobs;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.Modules.Evidence.Infrastructure.Repositories;
using Lagedra.Modules.Evidence.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.Evidence;

public static class EvidenceModuleRegistration
{
    public static IServiceCollection AddEvidence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<EvidenceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<EvidenceDbContext>();

        services.AddScoped<EvidenceManifestRepository>();
        services.AddScoped<IEvidenceManifestProvider, EvidenceManifestProvider>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(EvidenceModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var scanJobKey = new JobKey("MalwareScanPolling");
            q.AddJob<MalwareScanPollingJob>(opts => opts.WithIdentity(scanJobKey));
            q.AddTrigger(opts => opts
                .ForJob(scanJobKey)
                .WithIdentity("MalwareScanPolling-trigger")
                .WithSimpleSchedule(s => s.WithIntervalInMinutes(5).RepeatForever()));

            var retentionJobKey = new JobKey("EvidenceRetention");
            q.AddJob<EvidenceRetentionJob>(opts => opts.WithIdentity(retentionJobKey));
            q.AddTrigger(opts => opts
                .ForJob(retentionJobKey)
                .WithIdentity("EvidenceRetention-trigger")
                .WithCronSchedule("0 0 2 * * ?")); // Nightly at 2 AM
        });

        return services;
    }
}
