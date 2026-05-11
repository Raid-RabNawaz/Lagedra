using Lagedra.Modules.StructuredInquiry.Application.Commands;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.StructuredInquiry.Application.EventHandlers;

public sealed partial class OnTruthSurfaceConfirmedCloseInquiryHandler(
    IMediator mediator,
    ILogger<OnTruthSurfaceConfirmedCloseInquiryHandler> logger)
    : IDomainEventHandler<TruthSurfaceConfirmedEvent>
{
    public async Task Handle(TruthSurfaceConfirmedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogClosingInquiry(logger, domainEvent.DealId);

        var result = await mediator
            .Send(new CloseInquiryOnTruthSurfaceConfirmationCommand(domainEvent.DealId), ct)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            LogInquiryClosed(logger, domainEvent.DealId);
        }
        else
        {
            LogInquiryCloseSkipped(logger, domainEvent.DealId, result.Error.Description);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truth surface confirmed for deal {DealId}, closing inquiry session")]
    private static partial void LogClosingInquiry(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Inquiry session closed for deal {DealId}")]
    private static partial void LogInquiryClosed(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Could not close inquiry for deal {DealId}: {Reason}")]
    private static partial void LogInquiryCloseSkipped(ILogger logger, Guid dealId, string reason);
}
