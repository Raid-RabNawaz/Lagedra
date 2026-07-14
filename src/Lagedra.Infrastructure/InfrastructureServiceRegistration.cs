using Lagedra.Infrastructure.Behaviors;
using Lagedra.Infrastructure.Caching;
using Lagedra.Infrastructure.Eventing;
using Lagedra.Infrastructure.External.Antivirus;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.Hostaway;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Infrastructure.External.Email;
using Lagedra.Infrastructure.External.Geocoding;
using Lagedra.Infrastructure.External.Insurance;
using Lagedra.Infrastructure.External.Payments;
using Lagedra.Infrastructure.External.Kyc;
using Lagedra.Infrastructure.External.Persona;
using Lagedra.Infrastructure.External.Sms;
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
using Lagedra.SharedKernel.Sms;
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

        // SMS (Twilio Messaging API; no-op when credentials are missing)
        services.Configure<TwilioSettings>(
            configuration.GetSection(TwilioSettings.SectionName));
        var twilioSettings = configuration.GetSection(TwilioSettings.SectionName).Get<TwilioSettings>();
        if (twilioSettings?.IsConfigured == true)
        {
            services.AddHttpClient<ISmsService, TwilioSmsService>(client =>
            {
                client.BaseAddress = new Uri("https://api.twilio.com/2010-04-01/");
            });
        }
        else
        {
            services.AddScoped<ISmsService, NoOpSmsService>();
        }

        // Eventing
        services.AddScoped<IEventBus, InMemoryEventBus>();
        services.AddScoped<OutboxProcessor>();
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<OutboxDispatcher>();

        // Stripe
        services.AddOptions<StripeSettings>()
            .Bind(configuration.GetSection(StripeSettings.SectionName))
            .PostConfigure<IConfiguration>((options, config) =>
            {
                var frontend = (config["App:FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
                options.ConnectReturnUrl = new Uri($"{frontend}/app/payout-setup");
                options.ConnectRefreshUrl = new Uri($"{frontend}/app/payout-setup");
            });
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
        // resolves them by ProviderKey. Adding Guesty/Lodgify/etc. later is a
        // one-line addition here — no changes to the sync jobs or publisher.
        services.Configure<OwnerRezChannelSettings>(
            configuration.GetSection(OwnerRezChannelSettings.SectionName));
        services.Configure<HostawayChannelSettings>(
            configuration.GetSection(HostawayChannelSettings.SectionName));

        var ownerRezSettings = configuration
            .GetSection(OwnerRezChannelSettings.SectionName)
            .Get<OwnerRezChannelSettings>() ?? new OwnerRezChannelSettings();
        var hostawaySettings = configuration
            .GetSection(HostawayChannelSettings.SectionName)
            .Get<HostawayChannelSettings>() ?? new HostawayChannelSettings();

        // The OwnerRez provider talks to the HAXML/HAOLB channel API over a typed
        // HttpClient: channel-level credentials are sent as HTTP Basic auth and a
        // descriptive User-Agent is required by OwnerRez. Per-host scoping uses
        // the connection's advertiser id, not these static credentials.
        services.AddHttpClient<OwnerRezChannelProvider>(client =>
        {
            client.BaseAddress = ownerRezSettings.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ownerRezSettings.UserAgent);
            if (!string.IsNullOrWhiteSpace(ownerRezSettings.Username)
                && !string.IsNullOrWhiteSpace(ownerRezSettings.Key))
            {
                var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                    $"{ownerRezSettings.Username}:{ownerRezSettings.Key}"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {basic}");
            }
        });
        services.AddScoped<IChannelProvider>(sp => sp.GetRequiredService<OwnerRezChannelProvider>());

        // Hostaway uses per-host OAuth2 client credentials (account ID + API
        // secret on ChannelConnection). Only the API base URL is platform-level.
        services.AddHttpClient<HostawayChannelProvider>(client =>
        {
            client.BaseAddress = hostawaySettings.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", hostawaySettings.UserAgent);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-control", "no-cache");
        });
        services.AddScoped<IChannelProvider>(sp => sp.GetRequiredService<HostawayChannelProvider>());

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
