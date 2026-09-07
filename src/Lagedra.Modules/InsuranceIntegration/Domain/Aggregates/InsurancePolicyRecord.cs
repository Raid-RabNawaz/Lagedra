using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;

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
    public string? ExternalVerificationId { get; private set; }
    public string? ScreeningStatus { get; private set; }
    public string? FlaggedReason { get; private set; }

    public bool HasExternalVerification => !string.IsNullOrWhiteSpace(ExternalVerificationId);

    /// <summary>
    /// Reservation id last sent to Truvi. Stored on <see cref="PolicyNumber"/>
    /// so flagged re-screens can cancel/modify the replacement create.
    /// </summary>
    public string TruviReservationId =>
        !string.IsNullOrWhiteSpace(PolicyNumber) ? PolicyNumber : DealId.ToString("D");

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

    public void RecordScreeningResult(
        string verificationId,
        string screeningStatus,
        string? flaggedReason,
        DateTime? expiresAt,
        string? reservationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(screeningStatus);

        var oldState = State;
        ExternalVerificationId = verificationId;
        ScreeningStatus = screeningStatus;
        FlaggedReason = flaggedReason;
        Provider = "Truvi";
        if (!string.IsNullOrWhiteSpace(reservationId))
        {
            PolicyNumber = reservationId;
        }

        VerifiedAt = DateTime.UtcNow;
        UnknownSince = null;

        if (string.Equals(screeningStatus, TruviScreeningStatus.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            State = InsuranceState.NotActive;
            CoverageScope = "Truvi Rejected — unprotected";
            ExpiresAt = expiresAt;
            AddDomainEvent(new InsuranceStatusChangedEvent(DealId, oldState, InsuranceState.NotActive));
            return;
        }

        State = InsuranceState.Active;
        CoverageScope = string.Equals(screeningStatus, TruviScreeningStatus.Flagged, StringComparison.OrdinalIgnoreCase)
            ? "Truvi Complete Protection (Flagged)"
            : "Truvi Complete Protection";
        ExpiresAt = expiresAt;
        AddDomainEvent(new InsuranceStatusChangedEvent(DealId, oldState, InsuranceState.Active));
    }

    public void RecordScreeningFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        ScreeningStatus = TruviScreeningStatus.Failed;
        CoverageScope = Truncate(reason, 500);
        RecordUnknown();
    }

    public void MarkScreeningCancelled(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        ScreeningStatus = TruviScreeningStatus.Cancelled;
        if (State is InsuranceState.Active or InsuranceState.InstitutionBacked)
        {
            CancelPolicy(reason);
            return;
        }

        CoverageScope = Truncate($"Cancelled: {reason}", 500);
    }

    public void AddAttempt(InsuranceVerificationAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        _attempts.Add(attempt);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
