using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

public sealed record UpdateAmenityDefinitionCommand(
    Guid Id,
    string Name,
    AmenityCategory Category,
    string IconKey,
    bool IsActive,
    int SortOrder) : IRequest<Result<AmenityDefinitionDto>>;

public sealed class UpdateAmenityDefinitionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<UpdateAmenityDefinitionCommand, Result<AmenityDefinitionDto>>
{
    private static readonly Error NotFound = new("AmenityDefinition.NotFound", "Amenity definition not found.");

    public async Task<Result<AmenityDefinitionDto>> Handle(
        UpdateAmenityDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = await dbContext.AmenityDefinitions
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (definition is null)
        {
            return Result<AmenityDefinitionDto>.Failure(NotFound);
        }

        definition.Update(request.Name, request.Category, request.IconKey, request.IsActive, request.SortOrder);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AmenityDefinitionDto>.Success(
            new AmenityDefinitionDto(definition.Id, definition.Name, definition.Category, definition.IconKey));
    }
}
