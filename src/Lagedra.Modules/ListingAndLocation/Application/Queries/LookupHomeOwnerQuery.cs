using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record LookupHomeOwnerQuery(string Email, Guid CallerUserId)
    : IRequest<Result<ListingHomeOwnerDto>>;

public sealed class LookupHomeOwnerQueryHandler(IUserLookupService userLookup)
    : IRequestHandler<LookupHomeOwnerQuery, Result<ListingHomeOwnerDto>>
{
    public async Task<Result<ListingHomeOwnerDto>> Handle(
        LookupHomeOwnerQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<ListingHomeOwnerDto>.Failure(ListingManagementGuard.HomeOwnerRequired);
        }

        var account = await userLookup
            .FindAccountByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result<ListingHomeOwnerDto>.Failure(ListingManagementGuard.HomeOwnerNotFound);
        }

        if (account.UserId == request.CallerUserId)
        {
            return Result<ListingHomeOwnerDto>.Failure(ListingManagementGuard.HomeOwnerCannotBeSelf);
        }

        return Result<ListingHomeOwnerDto>.Success(
            new ListingHomeOwnerDto(account.UserId, account.DisplayName, account.Email));
    }
}
