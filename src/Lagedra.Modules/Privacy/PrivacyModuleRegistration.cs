using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.Modules.Privacy.Infrastructure.Repositories;
using Lagedra.Modules.Privacy.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IConsentChecker, ConsentChecker>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(PrivacyModuleRegistration).Assembly));

        return services;
    }
}
