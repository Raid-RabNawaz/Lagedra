namespace Lagedra.SharedKernel.Integration;

public sealed record HostStripeAccountDto(
    Guid HostUserId,
    string StripeAccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled);

public interface IHostStripeAccountProvider
{
    Task<HostStripeAccountDto?> GetByHostUserIdAsync(Guid hostUserId, CancellationToken ct = default);
}
