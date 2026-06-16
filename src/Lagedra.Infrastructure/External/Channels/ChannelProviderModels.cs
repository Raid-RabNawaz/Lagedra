namespace Lagedra.Infrastructure.External.Channels;

/// <summary>
/// Per-connection credentials passed to a provider on each call. Resolved from a
/// <c>ChannelConnection</c> (the secret is decrypted just-in-time). Provider
/// implementations combine these with their own static settings (base URL,
/// API version, …) when talking to the remote platform.
/// </summary>
public sealed record ChannelCredentials(
    string ProviderKey,
    string ExternalAccountId,
    string? Username = null,
    string? Secret = null,
    Uri? BaseUrl = null);

public sealed record ChannelAddress(
    string? Line1 = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null);

/// <summary>Normalized snapshot of an external listing pulled from a channel.</summary>
public sealed record ChannelListingSnapshot(
    string ExternalListingId,
    string Title,
    string? Description = null,
    long? MonthlyRentCents = null,
    long? NightlyRateCents = null,
    string Currency = "USD",
    int? MinStayNights = null,
    int? MaxStayNights = null,
    ChannelAddress? Address = null,
    IReadOnlyList<string>? AmenityCodes = null,
    IReadOnlyList<Uri>? PhotoUrls = null);

public sealed record ChannelDateBlock(DateOnly Start, DateOnly End, bool Available);

public sealed record ChannelAvailabilityCalendar(
    string ExternalListingId,
    IReadOnlyList<ChannelDateBlock> Blocks);

public sealed record ChannelAvailabilityQuery(
    string ExternalListingId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Adults = 1,
    int Children = 0,
    int Pets = 0);

public sealed record ChannelAvailabilityResult(bool Available, string? ErrorCode = null);

public sealed record ChannelGuest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone = null);

/// <summary>A single priced line on a pushed booking (rent, fee, tax, …).</summary>
public sealed record ChannelOrderItem(
    string Type,
    string Name,
    long AmountCents,
    string? ExternalId = null);

/// <summary>
/// A confirmed, already-paid booking to record on the channel. Lagedra is the
/// merchant of record, so <see cref="PaymentStatus"/> is typically "paid" and
/// no card / PAN data is included.
/// </summary>
public sealed record ChannelBookingPushRequest(
    string ExternalListingId,
    ChannelGuest Guest,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Adults,
    int Children,
    int Pets,
    string Currency,
    IReadOnlyList<ChannelOrderItem> OrderItems,
    string PaymentStatus,
    string TrackingReference,
    long? OwnerCommissionCents = null,
    long? GuestServiceFeeCents = null,
    string? Message = null);

public sealed record ChannelBookingPushResult(
    bool Success,
    string? ExternalBookingId = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record ChannelBookingUpdate(
    string ExternalBookingId,
    string Status,
    DateTime ChangedAtUtc);
