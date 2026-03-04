using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Aggregates;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Commands;

public sealed record DetectInquiryAbuseCommand(Guid SubjectUserId) : IRequest<Result>;

public sealed class DetectInquiryAbuseCommandHandler(
    IntegrityDbContext dbContext)
    : IRequestHandler<DetectInquiryAbuseCommand, Result>
{
    public async Task<Result> Handle(DetectInquiryAbuseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var abuseCase = AbuseCase.Open(request.SubjectUserId, AbuseType.InquiryAbuse);
        dbContext.AbuseCases.Add(abuseCase);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
