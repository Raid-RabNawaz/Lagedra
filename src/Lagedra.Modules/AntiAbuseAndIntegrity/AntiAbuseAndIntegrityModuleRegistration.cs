using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Repositories;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Services;
using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.AntiAbuseAndIntegrity;

public static class AntiAbuseAndIntegrityModuleRegistration
{
    public static IServiceCollection AddAntiAbuseAndIntegrity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<IntegrityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<IntegrityDbContext>();

        services.AddScoped<AbuseCaseRepository>();
        services.AddScoped<FraudFlagRepository>();
        services.AddScoped<IBookingRestrictionProvider, BookingRestrictionProvider>();

        // Notification handlers
        services.AddDomainEventHandler<Domain.Events.AccountRestrictionAppliedEvent,
            Application.EventHandlers.OnAccountRestrictionNotify>();
        services.AddDomainEventHandler<Lagedra.SharedKernel.Integration.Events.InquiryVolumeAbuseDetectedEvent,
            Application.EventHandlers.OnInquiryVolumeAbuseDetectedRaiseFlagHandler>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AntiAbuseAndIntegrityModuleRegistration).Assembly));

        return services;
    }
}
