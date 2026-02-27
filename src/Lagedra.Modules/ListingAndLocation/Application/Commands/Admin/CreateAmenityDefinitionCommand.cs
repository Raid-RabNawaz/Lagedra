using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

public sealed record CreateAmenityDefinitionCommand(
    string Name,
    AmenityCategory Category,
    string IconKey,
    int SortOrder) : IRequest<Result<AmenityDefinitionDto>>;

public sealed class CreateAmenityDefinitionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<CreateAmenityDefinitionCommand, Result<AmenityDefinitionDto>>
{
    public async Task<Result<AmenityDefinitionDto>> Handle(
        CreateAmenityDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = AmenityDefinition.Create(request.Name, request.Category, request.IconKey, request.SortOrder);

        dbContext.AmenityDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AmenityDefinitionDto>.Success(
            new AmenityDefinitionDto(definition.Id, definition.Name, definition.Category, definition.IconKey));
    }
}
