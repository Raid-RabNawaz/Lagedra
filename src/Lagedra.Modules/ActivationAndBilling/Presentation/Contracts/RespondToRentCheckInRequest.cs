namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record RespondToRentCheckInRequest(bool Received, string? Note = null);
