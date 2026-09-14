using System.Diagnostics.CodeAnalysis;

namespace Lagedra.SharedKernel.Email;

public sealed class EmailAttachment
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }

    [SuppressMessage(
        "Performance", "CA1819:Properties should not return arrays",
        Justification = "Email attachment content is a binary payload.")]
    public required byte[] Content { get; init; }

    /// <summary>
    /// When set, SendGrid treats this as an inline image referenced from HTML
    /// as <c>cid:{ContentId}</c> (used for the Lagedra logo).
    /// </summary>
    public string? ContentId { get; init; }

    /// <summary>
    /// SendGrid disposition. Defaults to <c>attachment</c>; inline images
    /// use <c>inline</c>.
    /// </summary>
    public string Disposition { get; init; } = "attachment";
}
