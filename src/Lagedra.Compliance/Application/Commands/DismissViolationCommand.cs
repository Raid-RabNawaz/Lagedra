using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.Commands;

public sealed record DismissViolationCommand(Guid ViolationId) : IRequest<Result>;

public sealed class DismissViolationCommandHandler(ComplianceDbContext dbContext)
    : IRequestHandler<DismissViolationCommand, Result>
{
    public async Task<Result> Handle(DismissViolationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violation = await dbContext.Violations
            .FirstOrDefaultAsync(v => v.Id == request.ViolationId, cancellationToken)
            .ConfigureAwait(false);

        if (violation is null)
        {
            return Result.Failure(new Error("Violation.NotFound", "Violation not found."));
        }

        violation.Dismiss();

        // The dismissal restores the trust the original entry took away, so
        // it is appended as its own ledger entry (the ledger is append-only —
        // the original violation entry is never removed).
        dbContext.TrustLedgerEntries.Add(TrustLedgerEntry.Create(
            violation.TargetUserId,
            TrustLedgerEntryType.ViolationDismissed,
            violation.Id,
            $"{violation.Category} violation dismissed",
            isPublic: false));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
