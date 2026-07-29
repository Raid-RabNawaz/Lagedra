namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns the generated lease PDF for a deal, generating and persisting it
/// on demand when it does not exist yet (e.g. the event-driven generation
/// after Truth Surface confirmation failed or has not run).
/// </summary>
public interface IDealLeasePdfService
{
    Task<DealLeaseDocument?> GetOrGenerateAsync(Guid dealId, CancellationToken ct = default);
}
