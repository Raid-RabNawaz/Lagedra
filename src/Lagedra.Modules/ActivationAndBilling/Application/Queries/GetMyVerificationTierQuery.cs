using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

/// <summary>
/// Resolves the caller's current verification tier ("trust level") using the
/// same resolver that selects predetermined deposits at booking time, so the
/// level shown to the user always matches what a host would see.
/// </summary>
public sealed record GetMyVerificationTierQuery(Guid UserId)
    : IRequest<Result<MyVerificationTierDto>>;

public sealed record MyVerificationTierDto(
    TenantVerificationTier Tier,
    Guid? PartnerOrganizationId);

public sealed class GetMyVerificationTierQueryHandler(
    ITenantVerificationTierResolver tierResolver)
    : IRequestHandler<GetMyVerificationTierQuery, Result<MyVerificationTierDto>>
{
    public async Task<Result<MyVerificationTierDto>> Handle(
        GetMyVerificationTierQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await tierResolver
            .ResolveAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        return Result<MyVerificationTierDto>.Success(
            new MyVerificationTierDto(result.Tier, result.PartnerOrganizationId));
    }
}
