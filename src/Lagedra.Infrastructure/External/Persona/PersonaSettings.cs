namespace Lagedra.Infrastructure.External.Persona;

public sealed class PersonaSettings
{
    public const string SectionName = "Persona";

    public required string ApiKey { get; init; }
    public required string TemplateId { get; init; }
    public required string WebhookSecret { get; init; }
    public Uri BaseUrl { get; init; } = new Uri("https://withpersona.com/api/v1");
}
