using Lagedra.Infrastructure.Eventing;
using Lagedra.TruthSurface.Infrastructure.Jobs;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using Lagedra.TruthSurface.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.TruthSurface;

public static class TruthSurfaceModuleRegistration
{
    public static IServiceCollection AddTruthSurface(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<TruthSurfaceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<TruthSurfaceDbContext>();

        services.AddScoped<SnapshotRepository>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TruthSurfaceModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("SnapshotVerification");
            q.AddJob<SnapshotVerificationJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("SnapshotVerification-trigger")
                .WithCronSchedule("0 0 3 ? * SUN")); // Every Sunday at 3 AM
        });

        return services;
    }
}
