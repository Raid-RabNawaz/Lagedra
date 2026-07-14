namespace Lagedra.Modules.ActivationAndBilling.Domain.Enums;

/// <summary>
/// Who is charged when a booking request is approved (V2 off-session charge).
/// The legal tenant on the deal is always <c>DealApplication.TenantUserId</c>;
/// this only selects the Stripe customer / payment method owner.
/// </summary>
public enum ApplicationPayerType
{
    Tenant = 0,
    PartnerOrganization = 1,
}
