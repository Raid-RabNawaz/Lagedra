using Lagedra.Infrastructure.Behaviors;
using Lagedra.Infrastructure.Caching;
using Lagedra.Infrastructure.Eventing;
using Lagedra.Infrastructure.External.Antivirus;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Infrastructure.External.Email;
using Lagedra.Infrastructure.External.Geocoding;
using Lagedra.Infrastructure.External.Insurance;
using Lagedra.Infrastructure.External.Payments;
using Lagedra.Infrastructure.External.Kyc;
using Lagedra.Infrastructure.External.Persona;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.SharedKernel.Integration;
using Lagedra.Infrastructure.Observability;
using Lagedra.Infrastructure.RealTime;
using Lagedra.Infrastructure.Security;
using Lagedra.Infrastructure.Settings;
using Lagedra.Infrastructure.Time;
using Lagedra.SharedKernel.Caching;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.RealTime;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        services.AddSingleton<IEncryptionService, EncryptionService>();

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

        // KYC Provider (provider-agnostic)
        var kycProvider = configuration.GetValue<string>("Kyc:Provider");
        if (string.Equals(kycProvider, "Persona", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<PersonaSettings>(
                configuration.GetSection(PersonaSettings.SectionName));
            services.AddHttpClient<IPersonaClient, PersonaClient>();
            services.AddScoped<IKycProvider, PersonaKycProvider>();
        }
        else
        {
            services.AddScoped<IKycProvider, NoOpKycProvider>();
        }

        // MinIO (Object Storage)
        services.Configure<MinioSettings>(
            configuration.GetSection(MinioSettings.SectionName));
        services.AddScoped<IObjectStorageService, MinioStorageService>();

        // ClamAV (Antivirus) — disabled in production when ClamAV daemon is unavailable
        services.Configure<ClamAvSettings>(
            configuration.GetSection(ClamAvSettings.SectionName));

        var clamAvEnabled = configuration.GetValue<bool?>($"{ClamAvSettings.SectionName}:Enabled") ?? true;
        if (clamAvEnabled)
        {
            services.AddScoped<IAntivirusService, ClamAvService>();
        }
        else
        {
            services.AddSingleton<IAntivirusService, NoOpAntivirusService>();
        }

        // Insurance (stub — replace when MGA partner is confirmed)
        services.AddScoped<IInsuranceApiClient, InsuranceApiClient>();

        // Channel / PMS integrations (provider-agnostic).
        // Register one IChannelProvider per integrated platform; the registry
        // resolves them by ProviderKey. Adding Hostaway/Guesty/etc. later is a
        // one-line addition here — no changes to the sync jobs or publisher.
        services.Configure<OwnerRezChannelSettings>(
            configuration.GetSection(OwnerRezChannelSettings.SectionName));
        services.AddScoped<IChannelProvider, OwnerRezChannelProvider>();
        services.AddScoped<IChannelProviderRegistry, ChannelProviderRegistry>();

        // Caching — swap InMemoryCacheService for a distributed impl (e.g. Redis) here
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, InMemoryCacheService>();

        // Platform Settings (DB-backed, admin-editable)
        services.AddDbContext<PlatformSettingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));
        services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();

        // Feature flags (Phase 16: BookingFlow.V2 rollout)
        services.AddSingleton<IFeatureFlags, FeatureFlags>();

        // Phase 16.10 — HMAC-signed one-tap action tokens for emails.
        // Falls back to the JWT secret if no dedicated secret is set so
        // single-secret dev environments keep working out of the box.
        services.Configure<ActionTokenSettings>(opts =>
        {
            var section = configuration.GetSection(ActionTokenSettings.SectionName);
            opts.Secret = section["Secret"]
                ?? configuration["Jwt:Secret"]
                ?? string.Empty;
        });
        services.AddSingleton<IActionTokenService, ActionTokenService>();

        // SignalR (real-time notifications)
        services.AddSignalR();
        services.AddSingleton<INotificationPusher, SignalRNotificationPusher>();

        // MediatR Pipeline Behaviors (order matters: validation → logging → exception handling)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Health Checks
        services.AddInfrastructureHealthChecks(configuration);

        return services;
    }
}
