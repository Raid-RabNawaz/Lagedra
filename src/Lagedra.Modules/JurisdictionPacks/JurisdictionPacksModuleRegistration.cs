using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.JurisdictionPacks.Application.EventHandlers;
using Lagedra.Modules.JurisdictionPacks.Domain.Events;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
