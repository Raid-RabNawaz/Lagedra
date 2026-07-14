using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

public sealed record GetInquiryThreadQuery(
    Guid DealId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<InquiryDto>>;

public sealed class GetInquiryThreadQueryHandler(
    InquiryDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<GetInquiryThreadQuery, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        GetInquiryThreadQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var participants = await dealStatusProvider
            .GetParticipantsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (!request.IsAdmin)
        {
            if (participants is null)
            {
                return Result<InquiryDto>.Failure(
                    new Error("Inquiry.NotFound", "No inquiry session found for this deal."));
            }

            if (participants.TenantUserId != request.CallerUserId
                && participants.LandlordUserId != request.CallerUserId)
            {
                return Result<InquiryDto>.Failure(
                    new Error("Inquiry.Forbidden",
                        "You do not have access to this deal's inquiry thread."));
            }
        }

        var session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Include(s => s.Offers)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(s => s.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "No inquiry session found for this deal."));
        }

        return Result<InquiryDto>.Success(
            InquiryDtoMapper.ToDto(session, landlordUserId: participants?.LandlordUserId));
    }
}
