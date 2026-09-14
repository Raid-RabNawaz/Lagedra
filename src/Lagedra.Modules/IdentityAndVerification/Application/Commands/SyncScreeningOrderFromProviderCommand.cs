using Lagedra.Modules.IdentityAndVerification.Application.Services;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScreeningPort = Lagedra.SharedKernel.Integration.Screening;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record SyncScreeningOrderFromProviderCommand(Guid ScreeningOrderId) : IRequest<Result>;

public sealed class SyncScreeningOrderFromProviderCommandHandler(
    IdentityDbContext dbContext,
    ScreeningPort.ITenantScreeningProvider screeningProvider,
    IMediator mediator)
    : IRequestHandler<SyncScreeningOrderFromProviderCommand, Result>
{
    public async Task<Result> Handle(
        SyncScreeningOrderFromProviderCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await dbContext.ScreeningOrders
            .FirstOrDefaultAsync(o => o.Id == request.ScreeningOrderId, cancellationToken)
            .ConfigureAwait(false);
        if (order is null || string.IsNullOrWhiteSpace(order.ExternalOrderId))
        {
            return Result.Success();
        }

        if (!ScreeningAccess.IsOpen(order.Status))
        {
            return Result.Success();
        }

        var remote = await screeningProvider
            .GetOrderAsync(order.ExternalOrderId, cancellationToken)
            .ConfigureAwait(false);
        if (remote is null)
        {
            return Result.Success();
        }

        var status = Map(remote.Status);
        var reportId = remote.ProviderReportId ?? order.ExternalReportId;
        order.ApplyProviderUpdate(status, remote.ApplicationUrl, reportId);

        ScreeningPort.ScreeningReportView? report = null;
        if (!string.IsNullOrWhiteSpace(reportId))
        {
            report = await screeningProvider.GetReportAsync(reportId, cancellationToken).ConfigureAwait(false);
        }

        report ??= await screeningProvider
            .GetOrderReportAsync(order.ExternalOrderId, cancellationToken)
            .ConfigureAwait(false);

        if (report is not null)
        {
            order.ApplyProviderUpdate(
                ScreeningOrderStatus.Completed,
                remote.ApplicationUrl,
                report.ProviderReportId);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (report is not null)
        {
            await mediator.Send(
                new IngestBackgroundCheckResultCommand(
                    order.TenantUserId,
                    report.ProviderReportId,
                    report.OverallResult),
                cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private static Domain.Enums.ScreeningOrderStatus Map(ScreeningPort.ScreeningOrderStatus status) =>
        status switch
        {
            ScreeningPort.ScreeningOrderStatus.WaitingForApplicant => ScreeningOrderStatus.WaitingForApplicant,
            ScreeningPort.ScreeningOrderStatus.Pending => ScreeningOrderStatus.Pending,
            ScreeningPort.ScreeningOrderStatus.Completed => ScreeningOrderStatus.Completed,
            ScreeningPort.ScreeningOrderStatus.Canceled => ScreeningOrderStatus.Canceled,
            ScreeningPort.ScreeningOrderStatus.Failed => ScreeningOrderStatus.Failed,
            ScreeningPort.ScreeningOrderStatus.Paid => ScreeningOrderStatus.Paid,
            _ => ScreeningOrderStatus.Pending
        };
}
