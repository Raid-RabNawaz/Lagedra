using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record GetApplicationStatusQuery(
    Guid ApplicationId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<DealApplicationDto>>;

public sealed class GetApplicationStatusQueryHandler(
    BillingDbContext dbContext)
    : IRequestHandler<GetApplicationStatusQuery, Result<DealApplicationDto>>
{
    public async Task<Result<DealApplicationDto>> Handle(
        GetApplicationStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<DealApplicationDto>.Failure(
                new Error("Application.NotFound", "Application not found."));
        }

        if (!request.IsAdmin
            && application.TenantUserId != request.CallerUserId
            && application.LandlordUserId != request.CallerUserId)
        {
            return Result<DealApplicationDto>.Failure(
                new Error("Application.Forbidden",
                    "You do not have access to this application."));
        }

        return Result<DealApplicationDto>.Success(DealApplicationDtoMapper.ToDto(application));
    }
}
