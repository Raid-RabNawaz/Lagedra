using System.Diagnostics.CodeAnalysis;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.Ai;

/// <summary>
/// Optional configuration for the AI-assisted enrichment of imported listing
/// drafts. The AI client is registered only when the
/// <c>ListingImport.AiExtraction</c> feature flag is enabled and a valid
/// <see cref="BaseUrl"/> is configured; otherwise the import falls back to
/// Open Graph/JSON-LD extraction only and behaviour is unchanged. Any
/// OpenAI-compatible chat-completions endpoint works (OpenAI, Azure OpenAI,
/// OpenRouter, and local servers such as Ollama or LM Studio).
///
/// For local models the <see cref="ApiKey"/> can be left blank (no bearer token
/// is sent); for cloud providers it is required.
/// </summary>
public sealed class ListingImportAiSettings
{
    public const string SectionName = "ListingImport:Ai";

    /// <summary>
    /// API key sent as a bearer token. Optional for local servers (Ollama /
    /// LM Studio), required for cloud providers.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base address of the OpenAI-compatible API, ending in <c>/v1</c>. The
    /// client appends <c>chat/completions</c>. Examples:
    /// <c>https://api.openai.com/v1</c>, <c>http://localhost:11434/v1</c>
    /// (Ollama), <c>http://localhost:1234/v1</c> (LM Studio).
    /// </summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
        Justification = "Bound from configuration; validated and converted to a Uri at registration.")]
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Chat model id used for extraction.</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Maximum characters of page context sent to the model. Kept moderate so
    /// local models stay responsive; a listing's key facts fit comfortably.
    /// </summary>
    public int MaxContextChars { get; set; } = 12000;

    /// <summary>
    /// Per-request timeout in seconds. Local models (especially on CPU) can be
    /// considerably slower than cloud APIs, so this defaults generously.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 60;
}
