using System.Security.Cryptography;
using System.Text;
using Lagedra.SharedKernel.Domain;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.Modules.VerificationAndRisk.Domain.Events;
using Lagedra.Modules.VerificationAndRisk.Domain.Policies;
using Lagedra.Modules.VerificationAndRisk.Domain.ValueObjects;

namespace Lagedra.Modules.VerificationAndRisk.Domain.Aggregates;

public sealed class RiskProfile : AggregateRoot<Guid>
{
    public Guid TenantUserId { get; private set; }
    public VerificationClass VerificationClass { get; private set; }
    public ConfidenceIndicator Confidence { get; private set; }
    public long DepositBandLowCents { get; private set; }
    public long DepositBandHighCents { get; private set; }
    public DateTime ComputedAt { get; private set; }
    public string InputHash { get; private set; }

#pragma warning disable CS8618
    private RiskProfile() { }
#pragma warning restore CS8618

    public static RiskProfile Create(Guid tenantUserId)
    {
        return new RiskProfile
        {
            Id = Guid.NewGuid(),
            TenantUserId = tenantUserId,
            VerificationClass = VerificationClass.High,
            Confidence = ConfidenceIndicator.Create(ConfidenceLevel.Low, "Not yet computed"),
            InputHash = string.Empty,
            ComputedAt = DateTime.UtcNow
        };
    }

    public void Recompute(VerificationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var hash = ComputeInputHash(input);
        if (string.Equals(hash, InputHash, StringComparison.Ordinal))
        {
            return;
        }

        var (verificationClass, confidence) = VerificationClassPolicy.Classify(input);

        VerificationClass = verificationClass;
        Confidence = confidence;
        InputHash = hash;
        ComputedAt = DateTime.UtcNow;

        AddDomainEvent(new VerificationClassComputedEvent(
            Id, TenantUserId, verificationClass, confidence.Level, ComputedAt));
    }

    public void UpdateDepositBand(InsuranceStatus insuranceStatus, long jurisdictionCapCents)
    {
        var (low, high) = DepositRecommendationPolicy.Recommend(
            VerificationClass, insuranceStatus, jurisdictionCapCents);

        DepositBandLowCents = low;
        DepositBandHighCents = high;

        AddDomainEvent(new DepositBandUpdatedEvent(Id, TenantUserId, low, high));
    }

    private static string ComputeInputHash(VerificationInput input)
    {
        var raw = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{(int)input.IdentityStatus}|{(int)input.BackgroundStatus}|{(int)input.InsuranceStatus}|{input.ViolationCount}");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(bytes);
    }
}
