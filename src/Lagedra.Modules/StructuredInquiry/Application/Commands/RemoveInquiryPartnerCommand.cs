using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record RemoveInquiryPartnerCommand(
    Guid SessionId,
    Guid CallerUserId) : IRequest<Result<InquiryDto>>;

public sealed class RemoveInquiryPartnerCommandHandler(
    InquiryDbContext dbContext,
    IPartnerMembershipProvider membershipProvider)
    : IRequestHandler<RemoveInquiryPartnerCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        RemoveInquiryPartnerCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "Inquiry thread not found."));
        }

        if (session.PartnerOrganizationId is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NoPartner", "No partner is attached to this inquiry."));
        }

        var isTenant = session.TenantUserId == request.CallerUserId;
        var callerOrgId = await membershipProvider
            .GetPartnerOrganizationIdAsync(request.CallerUserId, cancellationToken)
            .ConfigureAwait(false);
        var isPartnerStaff = callerOrgId == session.PartnerOrganizationId;

        if (!isTenant && !isPartnerStaff)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the tenant or the attached partner can remove the partner."));
        }

        try
        {
            session.RemovePartner(request.CallerUserId);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.PartnerConflict", ex.Message));
        }

        return Result<InquiryDto>.Success(InquiryDtoMapper.ToDto(session));
    }
}
