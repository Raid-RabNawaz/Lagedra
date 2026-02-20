using Lagedra.Infrastructure.Eventing;
using Lagedra.Infrastructure.External.Antivirus;
using Lagedra.Infrastructure.External.Email;
using Lagedra.Infrastructure.External.Geocoding;
using Lagedra.Infrastructure.External.Insurance;
using Lagedra.Infrastructure.External.Payments;
using Lagedra.Infrastructure.External.Persona;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Infrastructure.Observability;
using Lagedra.Infrastructure.Security;
using Lagedra.Infrastructure.Time;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Core
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IHashingService, HashingService>();
        services.AddSingleton<ICryptographicSigner, CryptographicSigner>();

        // Data Protection
        services.AddLagedraDataProtection();

        // Email
        services.Configure<BrevoSmtpSettings>(
            configuration.GetSection(BrevoSmtpSettings.SectionName));
        services.AddScoped<IEmailService, MailKitEmailService>();

        // Eventing
        services.AddScoped<IEventBus, InMemoryEventBus>();
        services.AddScoped<OutboxProcessor>();
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<OutboxDispatcher>();

        // Stripe
        services.Configure<StripeSettings>(
            configuration.GetSection(StripeSettings.SectionName));
        services.AddScoped<IStripeService, StripeService>();

        // Google Maps
        services.Configure<GoogleMapsSettings>(
            configuration.GetSection(GoogleMapsSettings.SectionName));
        services.AddHttpClient<IGeocodingService, GoogleMapsGeocodingService>();

        // Persona (KYC + Background Check)
        services.Configure<PersonaSettings>(
            configuration.GetSection(PersonaSettings.SectionName));
        services.AddHttpClient<IPersonaClient, PersonaClient>();

        // MinIO (Object Storage)
        services.Configure<MinioSettings>(
            configuration.GetSection(MinioSettings.SectionName));
        services.AddScoped<IObjectStorageService, MinioStorageService>();

        // ClamAV (Antivirus)
        services.Configure<ClamAvSettings>(
            configuration.GetSection(ClamAvSettings.SectionName));
        services.AddScoped<IAntivirusService, ClamAvService>();

        // Insurance (stub — replace when MGA partner is confirmed)
        services.AddScoped<IInsuranceApiClient, InsuranceApiClient>();

        // Health Checks
        services.AddInfrastructureHealthChecks(configuration);

        return services;
    }
}
