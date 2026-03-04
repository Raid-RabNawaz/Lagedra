using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Jobs;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Repositories;
using Lagedra.Infrastructure.Eventing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.AntiAbuseAndIntegrity;

public static class AntiAbuseAndIntegrityModuleRegistration
{
    public static IServiceCollection AddAntiAbuseAndIntegrity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<IntegrityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<IntegrityDbContext>();

        services.AddScoped<AbuseCaseRepository>();
        services.AddScoped<FraudFlagRepository>();

        // Notification handlers
        services.AddDomainEventHandler<Domain.Events.AccountRestrictionAppliedEvent,
            Application.EventHandlers.OnAccountRestrictionNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AntiAbuseAndIntegrityModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("PatternDetection");
            q.AddJob<PatternDetectionSchedulerJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("PatternDetection-trigger")
                .WithCronSchedule("0 0 */4 ? * *"));
        });

        return services;
    }
}
