using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Jobs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.ListingAndLocation;

public static class ListingAndLocationModuleRegistration
{
    public static IServiceCollection AddListingAndLocation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ListingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ListingsDbContext>();

        services.AddScoped<ListingRepository>();

        // Notification handlers
        services.AddDomainEventHandler<Domain.Events.ListingPublishedEvent,
            Application.EventHandlers.OnListingPublishedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ListingAndLocationModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("JurisdictionResolution");
            q.AddJob<JurisdictionResolutionJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("JurisdictionResolution-trigger")
                .WithCronSchedule("0 0 2 * * ?")); // Every night at 2 AM
        });

        return services;
    }
}
