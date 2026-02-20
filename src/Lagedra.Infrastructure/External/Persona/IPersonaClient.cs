namespace Lagedra.Infrastructure.External.Persona;

public enum PersonaInquiryStatus { Created, Pending, Completed, Failed, Expired }

public sealed record PersonaInquiry(
    string InquiryId,
    PersonaInquiryStatus Status,
    string? SessionToken,
    DateTime CreatedAt);

public interface IPersonaClient
{
    Task<PersonaInquiry> CreateInquiryAsync(Guid userId, string email, CancellationToken ct = default);
    Task<PersonaInquiry> GetInquiryAsync(string inquiryId, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
