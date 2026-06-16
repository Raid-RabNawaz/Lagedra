namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module hook implemented by <c>Lagedra.Modules.ChannelIntegration</c>.
///
/// Invoked by <c>ActivationAndBilling</c> once a deal's payment has been
/// confirmed, so the completed (already-paid) booking can be pushed back to the
/// external PMS / channel the listing originated from (e.g. OwnerRez). Because
/// Lagedra is the merchant of record, the booking is sent as "paid" and no card
/// data ever leaves Lagedra.
///
/// Implementations MUST be a no-op for deals whose listing is not linked to any
/// channel, and MUST treat channel delivery as best-effort: a failure here must
/// never roll back or disrupt the core payment / deal-activation flow.
/// </summary>
public interface IChannelBookingPublisher
{
    Task PublishConfirmedBookingAsync(Guid dealId, CancellationToken ct = default);
}
