namespace Lagedra.SharedKernel.Email;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
