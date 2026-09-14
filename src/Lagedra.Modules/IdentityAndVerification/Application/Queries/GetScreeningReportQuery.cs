using Lagedra.Modules.IdentityAndVerification.Application.Services;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Screening;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

public sealed record GetScreeningReportQuery(
    Guid ApplicationId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest<Result<ScreeningReportView>>;

public sealed class GetScreeningReportQueryHandler(
    IdentityDbContext dbContext,
    ITenantScreeningProvider screeningProvider)
    : IRequestHandler<GetScreeningReportQuery, Result<ScreeningReportView>>
{
    public async Task<Result<ScreeningReportView>> Handle(
        GetScreeningReportQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await dbContext.ScreeningOrders
            .AsNoTracking()
            .Where(o => o.ApplicationId == request.ApplicationId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (order is null || string.IsNullOrWhiteSpace(order.ExternalReportId))
        {
            return Result<ScreeningReportView>.Failure(
                new Error("Screening.ReportNotReady", "No screening report is available yet."));
        }

        if (!ScreeningAccess.CanViewReport(order, request.RequestingUserId, request.IsAdmin))
        {
            return Result<ScreeningReportView>.Failure(
                new Error("Application.Forbidden", "Only the host who paid can view this report."));
        }

        var report = await screeningProvider
            .GetReportAsync(order.ExternalReportId, cancellationToken)
            .ConfigureAwait(false);
        if (report is null)
        {
            return Result<ScreeningReportView>.Failure(
                new Error("Screening.ReportNotReady", "The provider has not published a report yet."));
        }

        return Result<ScreeningReportView>.Success(report);
    }
}
