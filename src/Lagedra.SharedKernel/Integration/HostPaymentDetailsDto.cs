namespace Lagedra.SharedKernel.Integration;

public sealed record HostPaymentDetailsDto(
    Guid HostUserId,
    string PaymentInfo);
