using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.PartnerNetwork;

public static class PartnerNetworkModuleRegistration
{
    public static IServiceCollection AddPartnerNetwork(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<PartnerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<PartnerDbContext>();

        services.AddScoped<IPartnerMembershipProvider, PartnerMembershipProvider>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(PartnerNetworkModuleRegistration).Assembly));

        return services;
    }
}
