using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Entities;

public sealed class HostPaymentDetails : Entity<Guid>
{
    public Guid HostUserId { get; private set; }
    public string EncryptedPaymentInfo { get; private set; } = string.Empty;

    private HostPaymentDetails() { }

    public static HostPaymentDetails Create(Guid hostUserId, string encryptedInfo, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedInfo);

        var now = clock.UtcNow;
        return new HostPaymentDetails
        {
            Id = Guid.NewGuid(),
            HostUserId = hostUserId,
            EncryptedPaymentInfo = encryptedInfo,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdatePaymentInfo(string encryptedInfo, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedInfo);

        EncryptedPaymentInfo = encryptedInfo;
        UpdatedAt = clock.UtcNow;
    }
}
