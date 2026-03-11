using Lagedra.Worker.Orchestration;
using Quartz;

namespace Lagedra.Worker.Scheduling;

internal static class QuartzSetup
{
    public static IServiceCollection AddQuartzScheduling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        services.AddQuartz(q =>
        {
            q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 10);

            q.UsePersistentStore(store =>
            {
                store.UsePostgres(connectionString);
                store.UseNewtonsoftJsonSerializer();
            });

            JobRegistry.RegisterAllJobs(q);

            q.AddJob<OutboxDispatchOrchestrator>(opts => opts
                .WithIdentity(nameof(OutboxDispatchOrchestrator)));
            q.AddTrigger(t => t
                .ForJob(nameof(OutboxDispatchOrchestrator))
                .WithIdentity($"{nameof(OutboxDispatchOrchestrator)}-trigger")
                .WithCronSchedule("0/10 * * * * ?"));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        services.AddSingleton<ModuleJobOrchestrator>();
        services.AddHostedService<HealthOrchestrator>();

        return services;
    }
}
