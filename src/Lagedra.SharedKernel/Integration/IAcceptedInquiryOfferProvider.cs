namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module hook for ActivationAndBilling to read an accepted inquiry
/// offer (negotiated rent + deposit) for a listing + tenant pair.
/// Implemented by StructuredInquiry.
/// </summary>
public interface IAcceptedInquiryOfferProvider
{
    /// <summary>
    /// Returns the accepted offer for the most relevant open (or deal-linked)
    /// inquiry session between this tenant and listing, or null if none.
    /// </summary>
    Task<AcceptedInquiryOfferDto?> GetAcceptedOfferAsync(
        Guid listingId,
        Guid tenantUserId,
        CancellationToken cancellationToken = default);
}

public sealed record AcceptedInquiryOfferDto(
    Guid OfferId,
    Guid SessionId,
    long RentCents,
    long DepositCents);
