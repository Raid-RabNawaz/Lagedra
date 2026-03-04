using Lagedra.Modules.ComplianceMonitoring.Domain.Entities;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ComplianceMonitoring.Application.Commands;

public sealed record RecordComplianceSignalCommand(
    Guid DealId,
    string SignalType,
    string Source) : IRequest<Result<Guid>>;

public sealed class RecordComplianceSignalCommandHandler(
    ComplianceMonitoringDbContext dbContext)
    : IRequestHandler<RecordComplianceSignalCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        RecordComplianceSignalCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var signal = MonitoredComplianceSignal.Record(
            request.DealId,
            request.SignalType,
            request.Source,
            DateTime.UtcNow);

        dbContext.Signals.Add(signal);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<Guid>.Success(signal.Id);
    }
}
