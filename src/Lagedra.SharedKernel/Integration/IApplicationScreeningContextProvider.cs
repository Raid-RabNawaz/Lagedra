namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Application participants needed to authorize and place a screening order
/// without the Identity module taking a dependency on Billing.
/// </summary>
public sealed record ApplicationScreeningContextDto(
    Guid ApplicationId,
    Guid TenantUserId,
    Guid LandlordUserId,
    Guid? HomeOwnerUserId,
    Guid ListingId,
    string Status);

public interface IApplicationScreeningContextProvider
{
    Task<ApplicationScreeningContextDto?> GetAsync(
        Guid applicationId,
        CancellationToken ct = default);
}
