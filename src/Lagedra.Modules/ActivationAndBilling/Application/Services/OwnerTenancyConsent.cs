using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Application.Services;

/// <summary>
/// Versioning for home-owner consent to a specific &gt;30-day tenancy when
/// a property manager lists the home. Bump <see cref="CurrentVersion"/> when
/// the consent wording changes.
/// </summary>
public static class OwnerTenancyConsent
{
    public const string CurrentVersion = "owner-tenancy-consent-v1";
    public const string EmailOneTapVersion = "owner-tenancy-consent-email-v1";

    public static bool IsRequired(ListingDetailsDto listing) =>
        string.Equals(listing.ManagerRole, "PropertyManager", StringComparison.OrdinalIgnoreCase)
        && listing.HomeOwnerUserId is { } ownerId
        && ownerId != Guid.Empty;

    public static void ApplyIfRequired(DealApplication application, ListingDetailsDto listing)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(listing);

        if (IsRequired(listing))
        {
            application.RequireOwnerConsent(listing.HomeOwnerUserId!.Value);
        }
    }
}
