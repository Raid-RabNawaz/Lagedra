using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

public sealed record CreateConsiderationDefinitionCommand(
    string Name,
    string IconKey,
    int SortOrder) : IRequest<Result<ConsiderationDefinitionDto>>;

public sealed class CreateConsiderationDefinitionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<CreateConsiderationDefinitionCommand, Result<ConsiderationDefinitionDto>>
{
    public async Task<Result<ConsiderationDefinitionDto>> Handle(
        CreateConsiderationDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = PropertyConsiderationDefinition.Create(request.Name, request.IconKey, request.SortOrder);

        dbContext.ConsiderationDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ConsiderationDefinitionDto>.Success(
            new ConsiderationDefinitionDto(definition.Id, definition.Name, definition.IconKey));
    }
}
