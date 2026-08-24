using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.Modules.Evidence.Application.Services;
using Lagedra.Modules.Evidence.Infrastructure.Repositories;
using Lagedra.Modules.Evidence.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.Evidence;

public static class EvidenceModuleRegistration
{
    public static IServiceCollection AddEvidence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<EvidenceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<EvidenceDbContext>();

        services.AddScoped<EvidenceManifestRepository>();
        services.AddScoped<IEvidenceManifestProvider, EvidenceManifestProvider>();
        services.AddScoped<EvidenceViewAccessService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(EvidenceModuleRegistration).Assembly));

        return services;
    }
}
