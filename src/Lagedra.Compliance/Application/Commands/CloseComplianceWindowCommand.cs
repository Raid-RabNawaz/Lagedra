using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.Commands;

public sealed record CloseComplianceWindowCommand(Guid DealId) : IRequest<Result>;

public sealed class CloseComplianceWindowCommandHandler(ComplianceDbContext dbContext)
    : IRequestHandler<CloseComplianceWindowCommand, Result>
{
    public async Task<Result> Handle(CloseComplianceWindowCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var openViolations = await dbContext.Violations
            .Where(v => v.DealId == request.DealId && v.Status == ViolationStatus.Open)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var violation in openViolations)
        {
            violation.Resolve();
        }

        var unprocessedSignals = await dbContext.Signals
            .Where(s => s.DealId == request.DealId && !s.Processed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var signal in unprocessedSignals)
        {
            signal.MarkProcessed();
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
