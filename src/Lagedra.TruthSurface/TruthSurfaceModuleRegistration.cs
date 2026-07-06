using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Integration;
using Lagedra.TruthSurface.Application.EventHandlers;
using Lagedra.TruthSurface.Application.Services;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.TruthSurface.Domain.Events;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using Lagedra.TruthSurface.Infrastructure.Repositories;
using Lagedra.TruthSurface.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<ITruthSurfaceStatusProvider, TruthSurfaceStatusProvider>();
        services.AddScoped<ITruthSurfaceSnapshotBuilder, TruthSurfaceSnapshotBuilder>();

        services.AddDomainEventHandler<TruthSurfaceInitiatedEvent, OnTruthSurfaceInitiatedNotify>();
        services.AddDomainEventHandler<TruthSurfaceConfirmedEvent, OnTruthSurfaceConfirmedNotify>();
        services.AddDomainEventHandler<TruthSurfaceSupersededEvent, OnTruthSurfaceSupersededNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TruthSurfaceModuleRegistration).Assembly));

        // Note: SnapshotVerificationJob is registered centrally in
        // Lagedra.Worker.Scheduling.JobRegistry to avoid double-scheduling.

        return services;
    }
}
