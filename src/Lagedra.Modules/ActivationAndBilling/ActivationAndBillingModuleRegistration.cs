using Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.ActivationAndBilling.Domain.Interfaces;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Repositories;
using Lagedra.Infrastructure.Eventing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Lagedra.TruthSurface.Domain.Events;

namespace Lagedra.Modules.ActivationAndBilling;

public static class ActivationAndBillingModuleRegistration
{
    public static IServiceCollection AddActivationAndBilling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<BillingDbContext>();

        services.AddScoped<DealApplicationRepository>();
        services.AddScoped<BillingAccountRepository>();
        services.AddScoped<InvoiceRepository>();
        services.AddScoped<IDealPaymentConfirmationRepository, DealPaymentConfirmationRepository>();

        services.AddDomainEventHandler<TruthSurfaceConfirmedEvent,
            OnTruthSurfaceConfirmedCreatePaymentConfirmationHandler>();
        services.AddDomainEventHandler<PaymentConfirmedEvent,
            OnPaymentConfirmedActivateDealHandler>();
        services.AddDomainEventHandler<PaymentDisputeResolvedEvent,
            OnPaymentDisputeResolvedHandler>();
        services.AddDomainEventHandler<BookingCancelledEvent,
            OnBookingCancelledCleanupHandler>();

        // Notification handlers
        services.AddDomainEventHandler<ApplicationSubmittedEvent,
            OnApplicationSubmittedNotify>();
        services.AddDomainEventHandler<ApplicationApprovedEvent,
            OnApplicationApprovedNotify>();
        services.AddDomainEventHandler<ApplicationRejectedEvent,
            OnApplicationRejectedNotify>();
        services.AddDomainEventHandler<PaymentConfirmedEvent,
            OnPaymentConfirmedNotify>();
        services.AddDomainEventHandler<PaymentDisputedEvent,
            OnPaymentDisputedNotify>();
        services.AddDomainEventHandler<PaymentDisputeResolvedEvent,
            OnPaymentDisputeResolvedNotify>();
        services.AddDomainEventHandler<DealActivatedEvent,
            OnDealActivatedNotify>();
        services.AddDomainEventHandler<BookingCancelledEvent,
            OnBookingCancelledNotify>();
        services.AddDomainEventHandler<DamageClaimFiledEvent,
            OnDamageClaimFiledNotify>();
        services.AddDomainEventHandler<PaymentFailedEvent,
            OnPaymentFailedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ActivationAndBillingModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var reconciliationKey = new JobKey("BillingReconciliation");
            q.AddJob<BillingReconciliationJob>(opts => opts.WithIdentity(reconciliationKey));
            q.AddTrigger(opts => opts
                .ForJob(reconciliationKey)
                .WithIdentity("BillingReconciliation-trigger")
                .WithCronSchedule("0 0 2 * * ?"));

            var timeoutKey = new JobKey("PaymentConfirmationTimeout");
            q.AddJob<PaymentConfirmationTimeoutJob>(opts => opts.WithIdentity(timeoutKey));
            q.AddTrigger(opts => opts
                .ForJob(timeoutKey)
                .WithIdentity("PaymentConfirmationTimeout-trigger")
                .WithCronSchedule("0 0 * * * ?"));

            var hostEnforcementKey = new JobKey("HostPlatformPaymentEnforcement");
            q.AddJob<HostPlatformPaymentEnforcementJob>(opts => opts.WithIdentity(hostEnforcementKey));
            q.AddTrigger(opts => opts
                .ForJob(hostEnforcementKey)
                .WithIdentity("HostPlatformPaymentEnforcement-trigger")
                .WithCronSchedule("0 0 8 * * ?"));
        });

        return services;
    }
}
