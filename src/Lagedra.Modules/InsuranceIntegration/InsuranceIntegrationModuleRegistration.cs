using Lagedra.SharedKernel.Integration.Events;
using Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;
using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Jobs;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Repositories;
using Lagedra.Infrastructure.Eventing;
using Lagedra.SharedKernel.Insurance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Services;

namespace Lagedra.Modules.InsuranceIntegration;

public static class InsuranceIntegrationModuleRegistration
{
    public static IServiceCollection AddInsuranceIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<InsuranceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<InsuranceDbContext>();

        services.AddScoped<InsurancePolicyRecordRepository>();
        services.AddScoped<IInsuranceStatusProvider, InsuranceStatusProvider>();
        services.AddScoped<Lagedra.SharedKernel.Integration.IUserInsuranceStatusProvider,
            UserInsuranceStatusProvider>();

        // Insurance fee calculator selection.
        //
        // Tenant rental insurance is no longer a host opt-in (the listing
        // flag was retired). Quotes go through whichever calculator is
        // registered here — so we default to a no-op "None" provider and
        // require operators to explicitly enable a real calculator before
        // any insurance line appears on bookings.
        //
        //   Insurance:FeeCalculationMode = "Api"          → live third-party connector
        //   Insurance:FeeCalculationMode = "Configurable" → built-in % of rent (dev/QA)
        //   anything else (incl. unset)                   → NullInsuranceFeeCalculator (FeeCents = 0)
        var feeMode = configuration["Insurance:FeeCalculationMode"];
        if (string.Equals(feeMode, "Api", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IInsuranceFeeCalculator, ApiInsuranceFeeCalculator>();
        }
        else if (string.Equals(feeMode, "Configurable", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IInsuranceFeeCalculator, ConfigurableInsuranceFeeCalculator>();
        }
        else
        {
            services.AddScoped<IInsuranceFeeCalculator, NullInsuranceFeeCalculator>();
        }

        services.AddDomainEventHandler<DealActivatedEvent,
            OnDealActivatedActivateInsuranceHandler>();

        services.AddDomainEventHandler<
            Lagedra.SharedKernel.Integration.Events.BookingCancelledEvent,
            OnBookingCancelledCancelInsuranceHandler>();

        services.AddDomainEventHandler<
            Lagedra.SharedKernel.Integration.Events.BillingStoppedEvent,
            OnBillingStoppedCancelInsuranceHandler>();

        // Notification handlers
        services.AddDomainEventHandler<
            Lagedra.SharedKernel.Integration.Events.InsuranceStatusChangedEvent,
            OnInsuranceStatusChangedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(InsuranceIntegrationModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var pollerKey = new JobKey("InsurancePoller");
            q.AddJob<InsurancePollerJob>(opts => opts.WithIdentity(pollerKey));
            q.AddTrigger(opts => opts
                .ForJob(pollerKey)
                .WithIdentity("InsurancePoller-trigger")
                .WithCronSchedule("0 0 * ? * *"));

            var slaKey = new JobKey("InsuranceUnknownSla");
            q.AddJob<InsuranceUnknownSlaJob>(opts => opts.WithIdentity(slaKey));
            q.AddTrigger(opts => opts
                .ForJob(slaKey)
                .WithIdentity("InsuranceUnknownSla-trigger")
                .WithCronSchedule("0 */30 * ? * *"));
        });

        return services;
    }
}
