using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

/// <summary>
/// The current user's uploaded manual-KYC documents (types + file names only —
/// no URLs; the documents live in a private bucket).
/// </summary>
public sealed record GetMyKycDocumentsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<KycDocumentDto>>>;

public sealed class GetMyKycDocumentsQueryHandler(IdentityDbContext dbContext)
    : IRequestHandler<GetMyKycDocumentsQuery, Result<IReadOnlyList<KycDocumentDto>>>
{
    public async Task<Result<IReadOnlyList<KycDocumentDto>>> Handle(
        GetMyKycDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documents = await dbContext.KycDocuments
            .AsNoTracking()
            .Where(d => d.UserId == request.UserId)
            .OrderBy(d => d.DocumentType)
            .Select(d => new KycDocumentDto(d.DocumentType, d.FileName, d.UploadedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<KycDocumentDto>>.Success(documents);
    }
}
