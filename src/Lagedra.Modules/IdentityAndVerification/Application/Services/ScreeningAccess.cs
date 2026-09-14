using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.IdentityAndVerification.Application.Services;

internal static class ScreeningAccess
{
    public static bool IsOpen(ScreeningOrderStatus? status) =>
        status is ScreeningOrderStatus.PendingPayment
            or ScreeningOrderStatus.Paid
            or ScreeningOrderStatus.WaitingForApplicant
            or ScreeningOrderStatus.Pending;

    public static bool CanRequest(ApplicationScreeningContextDto app, Guid userId, bool isAdmin) =>
        isAdmin || app.LandlordUserId == userId || app.HomeOwnerUserId == userId;

    public static bool CanViewReport(ScreeningOrder order, Guid userId, bool isAdmin) =>
        isAdmin || order.HostUserId == userId;

    public static bool CanViewStatus(ApplicationScreeningContextDto app, Guid userId, bool isAdmin) =>
        isAdmin || app.LandlordUserId == userId || app.HomeOwnerUserId == userId || app.TenantUserId == userId;
}
