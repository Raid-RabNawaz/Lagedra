namespace Lagedra.SharedKernel.Email;

public sealed class EmailMessage
{
    public required string To { get; init; }
    public string? ToName { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? PlainTextBody { get; init; }
    public string? ReplyTo { get; init; }
}
