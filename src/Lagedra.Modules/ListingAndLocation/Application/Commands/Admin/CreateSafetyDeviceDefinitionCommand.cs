using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

public sealed record CreateSafetyDeviceDefinitionCommand(
    string Name,
    string IconKey,
    int SortOrder) : IRequest<Result<SafetyDeviceDefinitionDto>>;

public sealed class CreateSafetyDeviceDefinitionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<CreateSafetyDeviceDefinitionCommand, Result<SafetyDeviceDefinitionDto>>
{
    public async Task<Result<SafetyDeviceDefinitionDto>> Handle(
        CreateSafetyDeviceDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = SafetyDeviceDefinition.Create(request.Name, request.IconKey, request.SortOrder);

        dbContext.SafetyDeviceDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<SafetyDeviceDefinitionDto>.Success(
            new SafetyDeviceDefinitionDto(definition.Id, definition.Name, definition.IconKey));
    }
}
