using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record IngestBackgroundCheckResultCommand(
    Guid UserId,
    string? PersonaReportId,
    BackgroundCheckResult Result) : IRequest<Result>;

public sealed class IngestBackgroundCheckResultCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<IngestBackgroundCheckResultCommand, Result>
{
    public async Task<Result> Handle(
        IngestBackgroundCheckResultCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = BackgroundCheckReport.Create(
            request.UserId, request.PersonaReportId, request.Result);

        dbContext.BackgroundCheckReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
