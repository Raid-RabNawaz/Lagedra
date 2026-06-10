using System.Diagnostics.CodeAnalysis;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.SharedKernel.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.Ai;

/// <summary>
/// Default AI enricher. Gated behind the <c>ListingImport.AiExtraction</c>
/// feature flag AND the presence of a configured <see cref="IChatClient"/>; if
/// either is missing it is a transparent no-op. It feeds the page's visible
/// text plus any JSON-LD blocks to the model and asks for the same suggestion
/// fields the structured extractor produces, then merges them in to fill gaps.
/// </summary>
public sealed partial class AiListingDraftEnricher : IListingDraftAiEnricher
{
    private const string FeatureFlag = "ListingImport.AiExtraction";

    private const string SystemPrompt =
        "You extract structured fields about a single rental/listing from raw web page content. " +
        "Use only facts explicitly present in the content. If a value is not clearly stated, return null. " +
        "Never guess prices, bedroom counts, bathroom counts, or capacity. " +
        "Times must be 24-hour HH:mm. Money fields are integer amounts in the smallest currency unit (cents). " +
        "Currency is a 3-letter ISO code. amenityHints are short human-readable labels (e.g. 'WiFi', 'Pool').";

    private readonly IChatClient? _chatClient;
    private readonly IFeatureFlags _featureFlags;
    private readonly ListingImportAiSettings _settings;
    private readonly ILogger<AiListingDraftEnricher> _logger;

    public AiListingDraftEnricher(
        IEnumerable<IChatClient> chatClients,
        IFeatureFlags featureFlags,
        IOptions<ListingImportAiSettings> options,
        ILogger<AiListingDraftEnricher> logger)
    {
        ArgumentNullException.ThrowIfNull(chatClients);
        ArgumentNullException.ThrowIfNull(featureFlags);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _chatClient = chatClients.FirstOrDefault();
        _featureFlags = featureFlags;
        _settings = options.Value;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "AI enrichment is best-effort and must never fail or block the import; any failure returns the original draft.")]
    public async Task<ImportedListingDraftDto> EnrichAsync(
        ImportedListingDraftDto draft,
        string html,
        Uri finalUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (_chatClient is null || !_featureFlags.IsEnabled(FeatureFlag) || string.IsNullOrWhiteSpace(html))
        {
            return draft;
        }

        try
        {
            var context = BuildContext(html, _settings.MaxContextChars);
            if (string.IsNullOrWhiteSpace(context))
            {
                return draft;
            }

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, "Extract the listing fields from the following page content:\n\n" + context),
            };

            var response = await _chatClient
                .GetResponseAsync<AiExtractedListing>(
                    messages,
                    useJsonSchemaResponseFormat: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Merge(draft, response.Result);
        }
        catch (Exception ex)
        {
            LogEnrichmentFailed(_logger, finalUrl.Host, ex);
            return draft;
        }
    }

    private static ImportedListingDraftDto Merge(ImportedListingDraftDto draft, AiExtractedListing? ai)
    {
        if (ai is null)
        {
            return draft;
        }

        return draft with
        {
            Title = draft.Title ?? Clean(ai.Title),
            Description = draft.Description ?? Clean(ai.Description),
            PropertyType = draft.PropertyType ?? Clean(ai.PropertyType),
            Bedrooms = draft.Bedrooms ?? ai.Bedrooms,
            Bathrooms = draft.Bathrooms ?? ai.Bathrooms,
            SquareFootage = draft.SquareFootage ?? ai.SquareFootage,
            MaxGuests = draft.MaxGuests ?? ai.MaxGuests,
            CheckInTime = draft.CheckInTime ?? Clean(ai.CheckInTime),
            CheckOutTime = draft.CheckOutTime ?? Clean(ai.CheckOutTime),
            MonthlyRentCents = draft.MonthlyRentCents ?? ai.MonthlyRentCents,
            NightlyRateCents = draft.NightlyRateCents ?? ai.NightlyRateCents,
            Currency = draft.Currency ?? Clean(ai.Currency),
            ApproxAddress = draft.ApproxAddress ?? Clean(ai.ApproxAddress),
            AmenityHints = MergeAmenities(draft.AmenityHints, ai.AmenityHints),
        };
    }

    private static IReadOnlyList<string>? MergeAmenities(
        IReadOnlyList<string>? existing,
        IReadOnlyList<string>? added)
    {
        if (added is not { Count: > 0 })
        {
            return existing;
        }

        var merged = new List<string>(existing ?? []);
        foreach (var amenity in added)
        {
            var trimmed = amenity?.Trim();
            if (!string.IsNullOrEmpty(trimmed) &&
                !merged.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                merged.Add(trimmed);
            }
        }

        return merged.Count > 0 ? merged : existing;
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string BuildContext(string html, int maxChars)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var builder = new StringBuilder();

        var title = document.Title;
        if (!string.IsNullOrWhiteSpace(title))
        {
            builder.Append("TITLE: ").AppendLine(title);
        }

        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            var json = script.TextContent?.Trim();
            if (!string.IsNullOrEmpty(json))
            {
                builder.AppendLine("JSON-LD:").AppendLine(json);
            }
        }

        var bodyText = ExtractVisibleText(document);
        if (!string.IsNullOrWhiteSpace(bodyText))
        {
            builder.AppendLine("PAGE TEXT:").AppendLine(bodyText);
        }

        var context = builder.ToString();
        return context.Length > maxChars ? context[..maxChars] : context;
    }

    private static string ExtractVisibleText(IDocument document)
    {
        foreach (var node in document.QuerySelectorAll("script, style, noscript, template, svg"))
        {
            node.Remove();
        }

        var text = document.Body?.TextContent ?? string.Empty;
        var collapsed = new StringBuilder(text.Length);
        var previousWasWhitespace = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasWhitespace)
                {
                    collapsed.Append(' ');
                    previousWasWhitespace = true;
                }
            }
            else
            {
                collapsed.Append(ch);
                previousWasWhitespace = false;
            }
        }

        return collapsed.ToString().Trim();
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "AI enrichment of listing import failed for {Host}; falling back to structured metadata only.")]
    private static partial void LogEnrichmentFailed(ILogger logger, string host, Exception exception);
}

/// <summary>
/// Schema the model fills. Scalars only and all nullable: no photo/URL fields
/// (those come from the structured extractor and re-upload pipeline), so the
/// model can never invent image links.
/// </summary>
public sealed record AiExtractedListing(
    string? Title,
    string? Description,
    string? PropertyType,
    int? Bedrooms,
    decimal? Bathrooms,
    int? SquareFootage,
    int? MaxGuests,
    string? CheckInTime,
    string? CheckOutTime,
    long? MonthlyRentCents,
    long? NightlyRateCents,
    string? Currency,
    IReadOnlyList<string>? AmenityHints,
    string? ApproxAddress);
