using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Application.Commands;

public sealed record ResolveViolationCommand(Guid ViolationId) : IRequest<Result>;

public sealed class ResolveViolationCommandHandler(ComplianceDbContext dbContext)
    : IRequestHandler<ResolveViolationCommand, Result>
{
    public async Task<Result> Handle(ResolveViolationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violation = await dbContext.Violations
            .FirstOrDefaultAsync(v => v.Id == request.ViolationId, cancellationToken)
            .ConfigureAwait(false);

        if (violation is null)
        {
            return Result.Failure(new Error("Violation.NotFound", "Violation not found."));
        }

        violation.Resolve();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
