using Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.Modules.ActivationAndBilling.Domain.Interfaces;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Repositories;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;
using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IDealApplicationStatusProvider, DealApplicationStatusProvider>();
        services.AddScoped<IPartnerDirectBookingService, PartnerDirectBookingService>();
        services.AddScoped<ICardOnFileChargeService, CardOnFileChargeService>();
        services.AddScoped<ITenantVerificationTierResolver, TenantVerificationTierResolver>();
        services.AddScoped<IReservationPricingService, ReservationPricingService>();

        services.AddDomainEventHandler<TruthSurfaceConfirmedEvent,
            OnTruthSurfaceConfirmedCreatePaymentConfirmationHandler>();
        services.AddDomainEventHandler<PaymentConfirmedEvent,
            OnPaymentConfirmedActivateDealHandler>();
        services.AddDomainEventHandler<PaymentConfirmedEvent,
            OnPaymentConfirmedPublishToChannelHandler>();
        services.AddDomainEventHandler<PaymentDisputeResolvedEvent,
            OnPaymentDisputeResolvedHandler>();
        services.AddDomainEventHandler<BookingCancelledEvent,
            OnBookingCancelledCleanupHandler>();

        // Notification handlers
        services.AddDomainEventHandler<ApplicationSubmittedEvent,
            OnApplicationSubmittedNotify>();
        services.AddDomainEventHandler<OwnerTenancyConsentGivenEvent,
            OnOwnerTenancyConsentGivenNotify>();
        services.AddDomainEventHandler<OwnerTenancyConsentDeclinedEvent,
            OnOwnerTenancyConsentDeclinedNotify>();
        services.AddDomainEventHandler<ApplicationApprovedEvent,
            OnApplicationApprovedNotify>();
        services.AddDomainEventHandler<ApplicationRejectedEvent,
            OnApplicationRejectedNotify>();
        services.AddDomainEventHandler<ApplicationExpiredEvent,
            OnApplicationExpiredNotify>();
        services.AddDomainEventHandler<ApplicationSupersededEvent,
            OnApplicationSupersededNotify>();
        services.AddDomainEventHandler<PaymentConfirmedEvent,
            OnPaymentConfirmedNotify>();
        services.AddDomainEventHandler<PaymentDisputedEvent,
            OnPaymentDisputedNotify>();
        services.AddDomainEventHandler<PaymentDisputeResolvedEvent,
            OnPaymentDisputeResolvedNotify>();
        services.AddDomainEventHandler<DealActivatedEvent,
            OnDealActivatedCreateHostSubscriptionHandler>();
        services.AddDomainEventHandler<DealActivatedEvent,
            OnDealActivatedNotify>();
        services.AddDomainEventHandler<StayCompletedEvent,
            OnStayCompletedStopBillingHandler>();
        services.AddDomainEventHandler<BookingCancelledEvent,
            OnBookingCancelledNotify>();
        services.AddDomainEventHandler<DamageClaimFiledEvent,
            OnDamageClaimFiledNotify>();
        services.AddDomainEventHandler<DamageClaimApprovedEvent,
            OnDamageClaimApprovedNotify>();
        services.AddDomainEventHandler<DamageClaimRejectedEvent,
            OnDamageClaimRejectedNotify>();
        services.AddDomainEventHandler<PaymentFailedEvent,
            OnPaymentFailedNotify>();
        services.AddDomainEventHandler<BookingPaymentFailedEvent,
            OnBookingPaymentFailedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ActivationAndBillingModuleRegistration).Assembly));

        return services;
    }
}
