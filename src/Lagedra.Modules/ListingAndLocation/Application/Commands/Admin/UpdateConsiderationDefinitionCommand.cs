using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

public sealed record UpdateConsiderationDefinitionCommand(
    Guid Id,
    string Name,
    string IconKey,
    bool IsActive,
    int SortOrder) : IRequest<Result<ConsiderationDefinitionDto>>;

public sealed class UpdateConsiderationDefinitionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<UpdateConsiderationDefinitionCommand, Result<ConsiderationDefinitionDto>>
{
    private static readonly Error NotFound = new("ConsiderationDefinition.NotFound", "Consideration definition not found.");

    public async Task<Result<ConsiderationDefinitionDto>> Handle(
        UpdateConsiderationDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = await dbContext.ConsiderationDefinitions
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (definition is null)
        {
            return Result<ConsiderationDefinitionDto>.Failure(NotFound);
        }

        definition.Update(request.Name, request.IconKey, request.IsActive, request.SortOrder);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ConsiderationDefinitionDto>.Success(
            new ConsiderationDefinitionDto(definition.Id, definition.Name, definition.IconKey));
    }
}
