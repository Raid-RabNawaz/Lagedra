using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.IdentityAndVerification.Application.DTOs;

public sealed record ScreeningCheckoutDto(
    string ClientSecret,
    string PaymentIntentId,
    string Status,
    long AmountCents,
    string Currency,
    Guid ScreeningOrderId);

public sealed record ScreeningStatusDto(
    Guid? ScreeningOrderId,
    ScreeningOrderStatus? Status,
    Uri? ApplicationUrl,
    DateTime? LastScreenedAt,
    BackgroundCheckResult? LatestResult,
    long HostFeeCents,
    bool CanRequest,
    bool CanViewReport,
    bool IsRequester = false,
    bool CanCompleteApply = false,
    string? LastFailureReason = null);
