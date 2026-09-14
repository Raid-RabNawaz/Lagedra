using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScreeningPort = Lagedra.SharedKernel.Integration.Screening;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record ProcessScreeningWebhookCommand(
    string Provider,
    string RawBody,
    IReadOnlyDictionary<string, string> Headers) : IRequest<Result>;

public sealed class ProcessScreeningWebhookCommandHandler(
    ScreeningPort.ITenantScreeningProvider screeningProvider,
    IdentityDbContext dbContext,
    IMediator mediator)
    : IRequestHandler<ProcessScreeningWebhookCommand, Result>
{
    public async Task<Result> Handle(
        ProcessScreeningWebhookCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.Provider, screeningProvider.ProviderKey, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Provider, "checkr", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Success();
        }

        var parsed = await screeningProvider
            .HandleWebhookAsync(request.RawBody, request.Headers, cancellationToken)
            .ConfigureAwait(false);
        if (parsed is null
            || (string.IsNullOrWhiteSpace(parsed.ProviderOrderId)
                && string.IsNullOrWhiteSpace(parsed.ProviderReportId)))
        {
            return Result.Success();
        }

        var order = await FindOrderAsync(parsed, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Success();
        }

        var status = parsed.OrderStatus is { } s
            ? Map(s)
            : order.Status;

        var reportId = parsed.ProviderReportId ?? order.ExternalReportId;
        var hadApplyUrl = order.ApplicationUrl is not null;
        order.ApplyProviderUpdate(status, parsed.ApplicationUrl, reportId);

        BackgroundCheckResult? result = parsed.OverallResult;
        if (status == ScreeningOrderStatus.Completed && result is null)
        {
            ScreeningPort.ScreeningReportView? report = null;
            if (!string.IsNullOrWhiteSpace(reportId))
            {
                report = await screeningProvider.GetReportAsync(reportId, cancellationToken).ConfigureAwait(false);
            }

            report ??= string.IsNullOrWhiteSpace(order.ExternalOrderId)
                ? null
                : await screeningProvider.GetOrderReportAsync(order.ExternalOrderId, cancellationToken)
                    .ConfigureAwait(false);

            if (report is not null)
            {
                result = report.OverallResult;
                reportId = report.ProviderReportId;
                order.ApplyProviderUpdate(status, parsed.ApplicationUrl, reportId);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!hadApplyUrl && order.ApplicationUrl is not null)
        {
            await mediator.Send(
                new NotifyScreeningApplyLinkCommand(order.Id),
                cancellationToken).ConfigureAwait(false);
        }

        if (status == ScreeningOrderStatus.Completed && result is { } mapped)
        {
            await mediator.Send(
                new IngestBackgroundCheckResultCommand(order.TenantUserId, reportId, mapped),
                cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private async Task<Domain.Entities.ScreeningOrder?> FindOrderAsync(
        ScreeningPort.ScreeningWebhookEvent parsed,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(parsed.ProviderOrderId))
        {
            var byOrder = await dbContext.ScreeningOrders
                .FirstOrDefaultAsync(o => o.ExternalOrderId == parsed.ProviderOrderId, cancellationToken)
                .ConfigureAwait(false);
            if (byOrder is not null)
            {
                return byOrder;
            }
        }

        if (!string.IsNullOrWhiteSpace(parsed.ProviderReportId))
        {
            var byReport = await dbContext.ScreeningOrders
                .FirstOrDefaultAsync(o => o.ExternalReportId == parsed.ProviderReportId, cancellationToken)
                .ConfigureAwait(false);
            if (byReport is not null)
            {
                return byReport;
            }

            var report = await screeningProvider
                .GetReportAsync(parsed.ProviderReportId, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(report?.ProviderOrderId))
            {
                return await dbContext.ScreeningOrders
                    .FirstOrDefaultAsync(o => o.ExternalOrderId == report.ProviderOrderId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return null;
    }

    private static ScreeningOrderStatus Map(ScreeningPort.ScreeningOrderStatus status) =>
        status switch
        {
            ScreeningPort.ScreeningOrderStatus.WaitingForApplicant => ScreeningOrderStatus.WaitingForApplicant,
            ScreeningPort.ScreeningOrderStatus.Pending => ScreeningOrderStatus.Pending,
            ScreeningPort.ScreeningOrderStatus.Completed => ScreeningOrderStatus.Completed,
            ScreeningPort.ScreeningOrderStatus.Canceled => ScreeningOrderStatus.Canceled,
            ScreeningPort.ScreeningOrderStatus.Failed => ScreeningOrderStatus.Failed,
            ScreeningPort.ScreeningOrderStatus.Paid => ScreeningOrderStatus.Paid,
            _ => ScreeningOrderStatus.PendingPayment
        };
}
