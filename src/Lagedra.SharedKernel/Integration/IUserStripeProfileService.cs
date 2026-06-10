namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Phase 16.9 — read/write companion to <see cref="IUserEmailResolver"/> that
/// surfaces the cached Stripe Customer id stored on the user account. The
/// booking pre-flight uses this to avoid re-creating a Stripe customer on
/// every apply. Implemented in <c>Lagedra.Auth</c>.
/// </summary>
public sealed record UserStripeProfile(
    Guid UserId,
    string Email,
    string? StripeCustomerId);

public interface IUserStripeProfileService
{
    Task<UserStripeProfile?> GetAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Persists the Stripe Customer id back on the user record. Safe to call
    /// repeatedly with the same id; replaces an existing value if Stripe
    /// returns a different customer (rare; mainly migration scenarios).
    /// </summary>
    Task SetStripeCustomerIdAsync(
        Guid userId,
        string stripeCustomerId,
        CancellationToken ct = default);
}
