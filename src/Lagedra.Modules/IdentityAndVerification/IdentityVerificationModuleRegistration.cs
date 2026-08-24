using Lagedra.Modules.IdentityAndVerification.Application.EventHandlers;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Repositories;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Services;
using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.IdentityAndVerification;

public static class IdentityVerificationModuleRegistration
{
    public static IServiceCollection AddIdentityVerification(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<IdentityDbContext>();

        services.AddScoped<IdentityProfileRepository>();
        services.AddScoped<VerificationCaseRepository>();
        services.AddScoped<HostPaymentDetailsRepository>();
        services.AddScoped<IHostPaymentDetailsProvider, HostPaymentDetailsProvider>();
        services.AddScoped<IHostStripeAccountProvider, HostStripeAccountProvider>();
        services.AddScoped<IHostVerificationProvider, HostVerificationProvider>();
        services.AddScoped<IVerificationSignalProvider, VerificationSignalProvider>();

        // Notification handlers
        services.AddDomainEventHandler<IdentityVerifiedEvent, OnIdentityVerifiedNotify>();

        // Sync IsGovernmentIdVerified flag to Auth
        services.AddDomainEventHandler<IdentityVerifiedEvent, OnIdentityVerifiedSyncAuthHandler>();

        // Sync Stripe connected account status when Stripe sends account.updated webhook
        services.AddDomainEventHandler<SharedKernel.Integration.StripeAccountUpdatedEvent,
            OnStripeAccountUpdatedSyncHandler>();
        services.AddDomainEventHandler<IdentityVerificationFailedEvent, OnIdentityVerificationFailedNotify>();
        services.AddDomainEventHandler<VerificationClassChangedEvent, OnVerificationClassChangedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(IdentityVerificationModuleRegistration).Assembly));

        return services;
    }
}
