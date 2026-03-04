using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Privacy.Infrastructure.Jobs;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.Modules.Privacy.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.Privacy;

public static class PrivacyModuleRegistration
{
    public static IServiceCollection AddPrivacy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<PrivacyDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<PrivacyDbContext>();

        services.AddScoped<ConsentRepository>();
        services.AddScoped<LegalHoldRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(PrivacyModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var retentionKey = new JobKey("RetentionEnforcement");
            q.AddJob<RetentionEnforcementJob>(opts => opts.WithIdentity(retentionKey));
            q.AddTrigger(opts => opts
                .ForJob(retentionKey)
                .WithIdentity("RetentionEnforcement-trigger")
                .WithCronSchedule("0 0 1 ? * *"));

            var purgeKey = new JobKey("DataExportPurge");
            q.AddJob<DataExportPurgeJob>(opts => opts.WithIdentity(purgeKey));
            q.AddTrigger(opts => opts
                .ForJob(purgeKey)
                .WithIdentity("DataExportPurge-trigger")
                .WithCronSchedule("0 0 2 ? * *"));
        });

        return services;
    }
}
