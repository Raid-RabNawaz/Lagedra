namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Partner-org billing profile (Stripe Customer) for company-pays reservations.
/// Implemented in <c>Lagedra.Modules.PartnerNetwork</c>.
/// </summary>
public interface IPartnerOrganizationBillingProfile
{
    Task<string?> GetNameAsync(Guid organizationId, CancellationToken ct = default);

    Task<string?> GetStripeCustomerIdAsync(Guid organizationId, CancellationToken ct = default);

    Task SetStripeCustomerIdAsync(
        Guid organizationId,
        string stripeCustomerId,
        CancellationToken ct = default);
}
