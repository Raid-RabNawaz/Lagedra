using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.Entities;

public sealed class ConsentRecord : Entity<Guid>
{
    public Guid UserConsentId { get; private set; }
    public ConsentType ConsentType { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;

    private ConsentRecord() { }

    public static ConsentRecord Create(
        Guid userConsentId,
        ConsentType consentType,
        string ipAddress,
        string userAgent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(userAgent);

        return new ConsentRecord
        {
            Id = Guid.NewGuid(),
            UserConsentId = userConsentId,
            ConsentType = consentType,
            GrantedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public void Withdraw()
    {
        if (WithdrawnAt is not null)
        {
            throw new InvalidOperationException("Consent has already been withdrawn.");
        }

        WithdrawnAt = DateTime.UtcNow;
    }
}
