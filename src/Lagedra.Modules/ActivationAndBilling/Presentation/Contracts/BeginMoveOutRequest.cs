namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

/// <summary>
/// Sent when a stay ends. <see cref="DocumentedNonpayment"/> is required only
/// when Eviction Protection is included and the stay is ending before the
/// scheduled check-out. True keeps a claim reviewable. False ends coverage
/// with the booking.
/// </summary>
public sealed record BeginMoveOutRequest(bool? DocumentedNonpayment = null);
