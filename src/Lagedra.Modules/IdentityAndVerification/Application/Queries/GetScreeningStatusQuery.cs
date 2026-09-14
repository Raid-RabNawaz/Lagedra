using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Application.Services;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

public sealed record GetScreeningStatusQuery(
    Guid ApplicationId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest<Result<ScreeningStatusDto>>;

public sealed class GetScreeningStatusQueryHandler(
    IdentityDbContext dbContext,
    IApplicationScreeningContextProvider applicationContext,
    IVerificationSignalProvider verificationSignals,
    IPlatformSettingsService platformSettings,
    IOptions<ScreeningOptions> screeningOptions,
    IMediator mediator)
    : IRequestHandler<GetScreeningStatusQuery, Result<ScreeningStatusDto>>
{
    public async Task<Result<ScreeningStatusDto>> Handle(
        GetScreeningStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var app = await applicationContext
            .GetAsync(request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);
        if (app is null)
        {
            return Result<ScreeningStatusDto>.Failure(
                new Error("Application.NotFound", "Application not found."));
        }

        if (!ScreeningAccess.CanViewStatus(app, request.RequestingUserId, request.IsAdmin))
        {
            return Result<ScreeningStatusDto>.Failure(
                new Error("Application.Forbidden", "You cannot view screening on this application."));
        }

        var order = await dbContext.ScreeningOrders
            .AsNoTracking()
            .Where(o => o.ApplicationId == request.ApplicationId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (order is not null && ScreeningAccess.IsOpen(order.Status) && !string.IsNullOrWhiteSpace(order.ExternalOrderId))
        {
            await mediator
                .Send(new SyncScreeningOrderFromProviderCommand(order.Id), cancellationToken)
                .ConfigureAwait(false);
            order = await dbContext.ScreeningOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken)
                .ConfigureAwait(false);
        }

        var signals = await verificationSignals
            .GetSignalsAsync(app.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var canRequest = ScreeningAccess.CanRequest(app, request.RequestingUserId, request.IsAdmin)
            && string.Equals(app.Status, "Pending", StringComparison.Ordinal)
            && !ScreeningAccess.IsOpen(order?.Status);
        var canViewReport = order is not null
            && ScreeningAccess.CanViewReport(order, request.RequestingUserId, request.IsAdmin)
            && !string.IsNullOrWhiteSpace(order.ExternalReportId)
            && order.Status == ScreeningOrderStatus.Completed;
        var isRequester = order is not null
            && (order.HostUserId == request.RequestingUserId || request.IsAdmin);
        var canCompleteApply = order?.ApplicationUrl is not null
            && app.TenantUserId == request.RequestingUserId;

        BackgroundCheckResult? latest = null;
        if (signals?.IsBackgroundCheckPassed == true)
        {
            latest = BackgroundCheckResult.Pass;
        }
        else if (signals?.IsBackgroundCheckUnderReview == true)
        {
            latest = BackgroundCheckResult.Review;
        }
        else if (signals?.IsBackgroundCheckFailed == true)
        {
            latest = BackgroundCheckResult.Fail;
        }

        var hostFeeCents = await platformSettings
            .GetLongAsync(PlatformSettingKeys.ScreeningHostFeeCents, screeningOptions.Value.HostFeeCents, cancellationToken)
            .ConfigureAwait(false);

        return Result<ScreeningStatusDto>.Success(new ScreeningStatusDto(
            order?.Id,
            order?.Status,
            canCompleteApply ? order?.ApplicationUrl : null,
            signals?.LastScreeningAt,
            latest,
            hostFeeCents,
            canRequest,
            canViewReport,
            isRequester,
            canCompleteApply,
            ScreeningAccess.CanRequest(app, request.RequestingUserId, request.IsAdmin)
                ? order?.LastFailureReason
                : null));
    }
}
