using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Notifications.Domain.Entities;

public sealed class NotificationTemplate : Entity<Guid>
{
    public string TemplateId { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string? PlainTextBody { get; private set; }

    private NotificationTemplate() { }

    public NotificationTemplate(
        string templateId,
        NotificationChannel channel,
        string subject,
        string htmlBody,
        string? plainTextBody = null)
        : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        TemplateId = templateId;
        Channel = channel;
        Subject = subject;
        HtmlBody = htmlBody;
        PlainTextBody = plainTextBody;
    }

    public string RenderSubject(Dictionary<string, string> payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return ReplacePlaceholders(Subject, payload);
    }

    public string RenderHtmlBody(Dictionary<string, string> payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return ReplacePlaceholders(HtmlBody, payload);
    }

    public string? RenderPlainTextBody(Dictionary<string, string> payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return PlainTextBody is not null ? ReplacePlaceholders(PlainTextBody, payload) : null;
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> payload)
    {
        var result = template;
        foreach (var kvp in payload)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
