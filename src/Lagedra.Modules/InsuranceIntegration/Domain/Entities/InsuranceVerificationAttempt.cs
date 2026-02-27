using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.InsuranceIntegration.Domain.Entities;

public sealed class InsuranceVerificationAttempt : Entity<Guid>
{
    public Guid PolicyRecordId { get; private set; }
    public DateTime AttemptedAt { get; private set; }
    public string Result { get; private set; } = string.Empty;
    public VerificationSource Source { get; private set; }

    private InsuranceVerificationAttempt() { }

    public InsuranceVerificationAttempt(
        Guid policyRecordId,
        string result,
        VerificationSource source)
        : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(result);
        PolicyRecordId = policyRecordId;
        AttemptedAt = DateTime.UtcNow;
        Result = result;
        Source = source;
    }
}
