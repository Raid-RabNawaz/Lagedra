using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.ChannelIntegration;

public static class ChannelIntegrationModuleRegistration
{
    public static IServiceCollection AddChannelIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ChannelDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ChannelDbContext>();

        // Pulls + imports external listing content into Lagedra (shared by the
        // scheduled content-sync job and the on-demand "sync now" command).
        services.AddScoped<ChannelContentImporter>();
        services.AddScoped<ChannelBookingUpdateReconciler>();

        // Enforces one connection per provider per host for every link path.
        services.AddScoped<ChannelConnectionLinker>();

        // Redirect URI + signed state shared by both ends of the OwnerRez
        // OAuth handshake.
        services.AddScoped<OwnerRezOAuthFlow>();

        // Cross-module hook: ActivationAndBilling pushes paid bookings here.
        services.AddScoped<IChannelBookingPublisher, ChannelBookingPublisher>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ChannelIntegrationModuleRegistration).Assembly));

        return services;
    }
}
