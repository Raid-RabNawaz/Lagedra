using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;

public sealed class InsurancePolicyRecord : AggregateRoot<Guid>
{
    public Guid TenantUserId { get; private set; }
    public Guid DealId { get; private set; }
    public InsuranceState State { get; private set; }
    public string? Provider { get; private set; }
    public string? PolicyNumber { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? CoverageScope { get; private set; }
    public DateTime? UnknownSince { get; private set; }

    private readonly List<InsuranceVerificationAttempt> _attempts = [];
    public IReadOnlyList<InsuranceVerificationAttempt> Attempts => _attempts.AsReadOnly();

    private InsurancePolicyRecord() { }

    public static InsurancePolicyRecord Create(Guid tenantUserId, Guid dealId)
    {
        return new InsurancePolicyRecord
        {
            Id = Guid.NewGuid(),
            TenantUserId = tenantUserId,
            DealId = dealId,
            State = InsuranceState.NotActive
        };
    }

    public void RecordActive(
        string? provider = null,
        string? policyNumber = null,
        string? coverageScope = null,
        DateTime? expiresAt = null)
    {
        var oldState = State;
        State = InsuranceState.Active;
        Provider = provider;
        PolicyNumber = policyNumber;
        CoverageScope = coverageScope;
        ExpiresAt = expiresAt;
        VerifiedAt = DateTime.UtcNow;
        UnknownSince = null;

        AddDomainEvent(new InsuranceStatusChangedEvent(DealId, oldState, InsuranceState.Active));
    }

    public void RecordNotActive()
    {
        var oldState = State;
        State = InsuranceState.NotActive;
        Provider = null;
        PolicyNumber = null;
        VerifiedAt = DateTime.UtcNow;
        UnknownSince = null;

        AddDomainEvent(new InsuranceStatusChangedEvent(DealId, oldState, InsuranceState.NotActive));
    }

    public void RecordUnknown()
    {
        var oldState = State;
        State = InsuranceState.Unknown;
        UnknownSince ??= DateTime.UtcNow;
        VerifiedAt = DateTime.UtcNow;

        AddDomainEvent(new InsuranceStatusChangedEvent(DealId, oldState, InsuranceState.Unknown));
    }

    public void MarkLapsed()
    {
        if (State != InsuranceState.Unknown)
        {
            throw new InvalidOperationException($"Can only lapse from Unknown state, current: '{State}'.");
        }

        var oldState = State;
        State = InsuranceState.NotActive;
        UnknownSince = null;

        AddDomainEvent(new InsuranceStatusChangedEvent(DealId, oldState, InsuranceState.NotActive));
    }

    public void RecordInstitutionBacked(
        string? provider = null,
        string? policyNumber = null,
        string? coverageScope = null,
        DateTime? expiresAt = null)
    {
        var oldState = State;
        State = InsuranceState.InstitutionBacked;
        Provider = provider;
        PolicyNumber = policyNumber;
        CoverageScope = coverageScope;
        ExpiresAt = expiresAt;
        VerifiedAt = DateTime.UtcNow;
        UnknownSince = null;

        AddDomainEvent(new InsuranceStatusChangedEvent(DealId, oldState, InsuranceState.InstitutionBacked));
    }

    public void CancelPolicy(string reason)
    {
        if (State is not (InsuranceState.Active or InsuranceState.InstitutionBacked))
        {
            return;
        }

        var oldState = State;
        State = InsuranceState.NotActive;
        CoverageScope = $"Cancelled: {reason}";
        ExpiresAt = DateTime.UtcNow;

        AddDomainEvent(new InsuranceStatusChangedEvent(DealId, oldState, InsuranceState.NotActive));
    }

    public void AddAttempt(InsuranceVerificationAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        _attempts.Add(attempt);
    }
}
