namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record PaymentDetailsDto(
    Guid DealId,
    string PaymentInfoPlain);
