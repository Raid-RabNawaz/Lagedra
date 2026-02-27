using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

public sealed record UpdateSafetyDeviceDefinitionCommand(
    Guid Id,
    string Name,
    string IconKey,
    bool IsActive,
    int SortOrder) : IRequest<Result<SafetyDeviceDefinitionDto>>;

public sealed class UpdateSafetyDeviceDefinitionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<UpdateSafetyDeviceDefinitionCommand, Result<SafetyDeviceDefinitionDto>>
{
    private static readonly Error NotFound = new("SafetyDeviceDefinition.NotFound", "Safety device definition not found.");

    public async Task<Result<SafetyDeviceDefinitionDto>> Handle(
        UpdateSafetyDeviceDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = await dbContext.SafetyDeviceDefinitions
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (definition is null)
        {
            return Result<SafetyDeviceDefinitionDto>.Failure(NotFound);
        }

        definition.Update(request.Name, request.IconKey, request.IsActive, request.SortOrder);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<SafetyDeviceDefinitionDto>.Success(
            new SafetyDeviceDefinitionDto(definition.Id, definition.Name, definition.IconKey));
    }
}
