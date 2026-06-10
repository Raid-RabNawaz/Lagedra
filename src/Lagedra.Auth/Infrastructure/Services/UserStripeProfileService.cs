using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Integration;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Infrastructure.Services;

/// <summary>
/// Auth-side implementation of the Phase 16.9 user → Stripe-customer
/// integration. Reads/writes <see cref="ApplicationUser.StripeCustomerId"/>
/// directly via <see cref="UserManager{TUser}"/> so consumers in
/// ActivationAndBilling don't need a hard dependency on Auth's identity
/// types.
/// </summary>
public sealed class UserStripeProfileService(UserManager<ApplicationUser> userManager)
    : IUserStripeProfileService
{
    public async Task<UserStripeProfile?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null || string.IsNullOrEmpty(user.Email))
        {
            return null;
        }

        return new UserStripeProfile(userId, user.Email, user.StripeCustomerId);
    }

    public async Task SetStripeCustomerIdAsync(
        Guid userId,
        string stripeCustomerId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeCustomerId);

        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return;
        }

        if (string.Equals(user.StripeCustomerId, stripeCustomerId, StringComparison.Ordinal))
        {
            return;
        }

        user.StripeCustomerId = stripeCustomerId;
        await userManager.UpdateAsync(user).ConfigureAwait(false);
    }
}
