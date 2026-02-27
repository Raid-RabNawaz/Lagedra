using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record CreateCollectionCommand(Guid UserId, string Name)
    : IRequest<Result<SavedListingCollectionDto>>;

public sealed class CreateCollectionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<CreateCollectionCommand, Result<SavedListingCollectionDto>>
{
    public async Task<Result<SavedListingCollectionDto>> Handle(
        CreateCollectionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var collection = SavedListingCollections.Create(request.UserId, request.Name);
        dbContext.SavedListingCollections.Add(collection);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<SavedListingCollectionDto>.Success(
            new SavedListingCollectionDto(collection.Id, collection.Name, collection.CreatedAt, 0));
    }
}
