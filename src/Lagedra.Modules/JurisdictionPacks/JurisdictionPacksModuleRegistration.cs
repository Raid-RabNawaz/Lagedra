using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.JurisdictionPacks.Application.EventHandlers;
using Lagedra.Modules.JurisdictionPacks.Domain.Events;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Jobs;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.JurisdictionPacks;

public static class JurisdictionPacksModuleRegistration
{
    public static IServiceCollection AddJurisdictionPacks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<JurisdictionDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<JurisdictionDbContext>();

        services.AddScoped<JurisdictionPackRepository>();
        services.AddScoped<IJurisdictionPackProvider, JurisdictionPackProvider>();

        services.AddDomainEventHandler<JurisdictionPackPublishedEvent, OnPackPublishedInvalidateCacheHandler>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(JurisdictionPacksModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("PackEffectiveDateActivation");
            q.AddJob<PackEffectiveDateActivationJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("PackEffectiveDateActivation-trigger")
                .WithCronSchedule("0 0 0 * * ?")); // Daily at midnight
        });

        return services;
    }
}
