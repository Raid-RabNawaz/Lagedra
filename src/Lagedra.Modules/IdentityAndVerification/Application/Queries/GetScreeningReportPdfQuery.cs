using Lagedra.Modules.IdentityAndVerification.Application.Services;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration.Screening;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

public sealed record GetScreeningReportPdfQuery(
    Guid ApplicationId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest<Result<ScreeningReportPdf>>;

public sealed class GetScreeningReportPdfQueryHandler(
    IdentityDbContext dbContext,
    ITenantScreeningProvider screeningProvider)
    : IRequestHandler<GetScreeningReportPdfQuery, Result<ScreeningReportPdf>>
{
    public async Task<Result<ScreeningReportPdf>> Handle(
        GetScreeningReportPdfQuery request,
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
            return Result<ScreeningReportPdf>.Failure(
                new Error("Screening.ReportNotReady", "No screening report is available yet."));
        }

        if (!ScreeningAccess.CanViewReport(order, request.RequestingUserId, request.IsAdmin))
        {
            return Result<ScreeningReportPdf>.Failure(
                new Error("Application.Forbidden", "Only the host who paid can view this report."));
        }

        var pdf = await screeningProvider
            .GetReportPdfAsync(order.ExternalReportId, cancellationToken)
            .ConfigureAwait(false);
        if (pdf is null)
        {
            return Result<ScreeningReportPdf>.Failure(
                new Error("Screening.ReportNotReady", "The provider has not published a PDF yet."));
        }

        return Result<ScreeningReportPdf>.Success(pdf);
    }
}
