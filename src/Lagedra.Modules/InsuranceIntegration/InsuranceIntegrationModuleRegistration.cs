using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;
using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Repositories;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Services;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddScoped<IInsurancePolicyRecordStore>(sp =>
            sp.GetRequiredService<InsurancePolicyRecordRepository>());
        services.Configure<TruviScreenAndProtectSettings>(
            configuration.GetSection(TruviScreenAndProtectSettings.SectionName));
        services.AddHttpClient<ITruviScreenAndProtectClient, TruviScreenAndProtectClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TruviScreenAndProtectSettings>>().Value;
            client.BaseAddress = EnsureTrailingSlash(options.BaseUrl);
        });
        services.AddScoped<TruviScreeningService>();
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
        //   Insurance:FeeCalculationMode = "Truvi"        → nightly stay-protection recovery
        //   Insurance:FeeCalculationMode = "Api"          → live third-party /v1/quotes connector
        //   Insurance:FeeCalculationMode = "Configurable" → built-in % of rent (dev/QA)
        //   anything else (incl. unset)                   → NullInsuranceFeeCalculator (FeeCents = 0)
        var feeMode = configuration["Insurance:FeeCalculationMode"];
        if (string.Equals(feeMode, "Truvi", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IInsuranceFeeCalculator, TruviStayProtectionFeeCalculator>();
        }
        else if (string.Equals(feeMode, "Api", StringComparison.OrdinalIgnoreCase))
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

        services.AddDomainEventHandler<TruthSurfaceConfirmedEvent,
            OnTruthSurfaceConfirmedRequestTruviVerificationHandler>();

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

        return services;
    }

    private static Uri EnsureTrailingSlash(Uri baseUrl)
    {
        var text = baseUrl.ToString();
        return text.EndsWith('/') ? baseUrl : new Uri(text + "/");
    }
}
