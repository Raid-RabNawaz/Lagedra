namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Phase 17 — cross-module hook for the booking flow to attach a tenant's
/// pre-existing pre-booking inquiry thread to the deal that just got
/// created from their application. Implemented by the StructuredInquiry
/// module; consumed by ActivationAndBilling.
/// </summary>
public interface IInquiryDealLinker
{
    /// <summary>
    /// Find the calling tenant's open listing-scoped inquiry for
    /// <paramref name="listingId"/> and link it to <paramref name="dealId"/>.
    /// No-op if no matching session exists; idempotent if the session is
    /// already linked to the same deal.
    /// </summary>
    Task LinkOpenInquiryToDealAsync(
        Guid listingId,
        Guid tenantUserId,
        Guid dealId,
        CancellationToken ct = default);
}
