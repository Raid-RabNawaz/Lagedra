namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Whether a user is suspended or banned from booking. Implemented by AntiAbuse.
/// </summary>
public interface IBookingRestrictionProvider
{
    Task<bool> IsBlockedFromBookingAsync(Guid userId, CancellationToken ct = default);
}
