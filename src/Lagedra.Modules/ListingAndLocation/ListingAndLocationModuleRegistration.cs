using Lagedra.Infrastructure.Eventing;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.Ai;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.ScrapingAnt;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Jobs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Repositories;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Lagedra.Modules.ListingAndLocation;

public static class ListingAndLocationModuleRegistration
{
    public static IServiceCollection AddListingAndLocation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ListingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddOutboxContext<ListingsDbContext>();

        services.AddScoped<ListingRepository>();
        services.AddScoped<IListingProvider, ListingProvider>();

        // Cross-module importer used by ChannelIntegration to materialise
        // externally-sourced listings (e.g. OwnerRez) into Lagedra drafts.
        services.AddScoped<IListingImporter, ListingImporter>();

        // "Import from URL" pre-fill: server-side fetcher + Open Graph/JSON-LD
        // extractor. Mirrors the typed-HttpClient pattern used for IGeocodingService.
        services.AddSingleton<IListingMetadataExtractor, OpenGraphJsonLdExtractor>();

        // The page fetcher is pluggable. When a ScrapingAnt API key is configured
        // we route fetches through ScrapingAnt's headless-browser + proxy API so
        // JavaScript-rendered / bot-protected listings (e.g. Airbnb) resolve to
        // real HTML. Otherwise we use the plain HttpClient fetcher. Both satisfy
        // IListingImportClient, so the extractor/enricher pipeline is unchanged.
        var scrapingAntSettings = configuration
            .GetSection(ScrapingAntSettings.SectionName)
            .Get<ScrapingAntSettings>();
        services.Configure<ScrapingAntSettings>(
            configuration.GetSection(ScrapingAntSettings.SectionName));

        if (!string.IsNullOrWhiteSpace(scrapingAntSettings?.ApiKey))
        {
            var apiKey = scrapingAntSettings.ApiKey;
            var renderTimeout = Math.Clamp(scrapingAntSettings.TimeoutSeconds, 5, 60);
            services.AddHttpClient<IListingImportClient, ScrapingAntListingImportClient>(client =>
            {
                client.BaseAddress = new Uri(EnsureTrailingSlash(scrapingAntSettings.BaseUrl));
                // Allow margin over ScrapingAnt's own render timeout so the
                // HttpClient does not abort a still-valid response.
                client.Timeout = TimeSpan.FromSeconds(renderTimeout + 20);
                client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);
            });
        }
        else
        {
            services.AddHttpClient<IListingImportClient, HttpListingImportClient>(client =>
            {
                client.Timeout = ListingImportPolicy.FetchTimeout;
                client.MaxResponseContentBufferSize = ListingImportPolicy.MaxResponseBytes;
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ListingImportPolicy.UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = ListingImportPolicy.MaxRedirectDepth,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
            });
        }

        // Optional AI enrichment for the "import from URL" pre-fill. The enricher
        // is always registered but is a transparent no-op unless the
        // ListingImport.AiExtraction feature flag is on AND a chat client is
        // wired below. It only fills gaps the structured extractor missed;
        // nothing is persisted.
        services.Configure<ListingImportAiSettings>(
            configuration.GetSection(ListingImportAiSettings.SectionName));
        services.AddTransient<IListingDraftAiEnricher, AiListingDraftEnricher>();

        // Wire the chat client when the feature flag is enabled and a valid
        // endpoint is configured. The API key is optional so local servers
        // (Ollama / LM Studio) work without one; cloud providers supply a key
        // that is sent as a bearer token.
        var aiSettings = configuration.GetSection(ListingImportAiSettings.SectionName)
            .Get<ListingImportAiSettings>() ?? new ListingImportAiSettings();
        var aiEnabled = configuration.GetValue<bool>("FeatureFlags:ListingImport.AiExtraction");
        if (aiEnabled &&
            Uri.TryCreate(EnsureTrailingSlash(aiSettings.BaseUrl), UriKind.Absolute, out var aiBaseUri))
        {
            var apiKey = aiSettings.ApiKey;
            var aiTimeout = Math.Clamp(aiSettings.RequestTimeoutSeconds, 10, 300);
            services.AddHttpClient<IChatClient, OpenAiCompatibleChatClient>(client =>
            {
                client.BaseAddress = aiBaseUri;
                client.Timeout = TimeSpan.FromSeconds(aiTimeout);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                }
            });
        }

        // Notification handlers
        services.AddDomainEventHandler<Domain.Events.ListingSubmittedForReviewEvent,
            Application.EventHandlers.OnListingSubmittedForReviewNotify>();
        services.AddDomainEventHandler<Domain.Events.ListingPublishedEvent,
            Application.EventHandlers.OnListingPublishedNotify>();
        services.AddDomainEventHandler<Domain.Events.ListingDeniedEvent,
            Application.EventHandlers.OnListingDeniedNotify>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ListingAndLocationModuleRegistration).Assembly));

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("JurisdictionResolution");
            q.AddJob<JurisdictionResolutionJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("JurisdictionResolution-trigger")
                .WithCronSchedule("0 0 2 * * ?")); // Every night at 2 AM
        });

        return services;
    }

    private static string EnsureTrailingSlash(string url) =>
        url.EndsWith('/') ? url : url + "/";
}
