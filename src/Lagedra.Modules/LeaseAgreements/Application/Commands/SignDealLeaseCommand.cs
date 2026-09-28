using Lagedra.Modules.LeaseAgreements.Application.DTOs;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

public sealed record SignDealLeaseCommand(
    Guid DealId,
    Guid CallerUserId,
    string TypedName,
    bool Accepted) : IRequest<Result<DealLeaseSigningStatusDto>>;

public sealed class SignDealLeaseCommandHandler(
    IDealApplicationStatusProvider deals,
    IListingProvider listings,
    IDealLeaseSignatureStore signatures,
    IDealLeasePdfService pdfs,
    IClock clock)
    : IRequestHandler<SignDealLeaseCommand, Result<DealLeaseSigningStatusDto>>
{
    private static readonly Error NotFound = new("LeaseAgreement.DealNotFound", "Deal not found.");
    private static readonly Error Forbidden = new(
        "LeaseAgreement.Forbidden",
        "Only the host or tenant on this deal can sign the lease.");
    private static readonly Error NotRequired = new(
        "LeaseAgreement.SigningNotRequired",
        "This listing does not require in-app lease signatures.");
    private static readonly Error NotAccepted = new(
        "LeaseAgreement.SignatureNotAccepted",
        "Confirm that you are signing this lease electronically.");
    private static readonly Error NameRequired = new(
        "LeaseAgreement.SignatureNameRequired",
        "Type your name to sign this lease.");
    private static readonly Error PdfNotReady = new(
        "LeaseAgreement.PdfNotReady",
        "The lease PDF is not ready to sign yet. Try again in a moment.");

    public async Task<Result<DealLeaseSigningStatusDto>> Handle(
        SignDealLeaseCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Accepted)
        {
            return Result<DealLeaseSigningStatusDto>.Failure(NotAccepted);
        }

        var typedName = request.TypedName?.Trim() ?? string.Empty;
        if (typedName.Length < 2)
        {
            return Result<DealLeaseSigningStatusDto>.Failure(NameRequired);
        }

        var participants = await deals.GetParticipantsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);
        if (participants is null)
        {
            return Result<DealLeaseSigningStatusDto>.Failure(NotFound);
        }

        var callerRole = DealLeaseSigning.RoleFor(request.CallerUserId, participants);
        if (callerRole is null)
        {
            return Result<DealLeaseSigningStatusDto>.Failure(Forbidden);
        }

        var deal = await deals.GetDealDetailsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);
        if (deal is null)
        {
            return Result<DealLeaseSigningStatusDto>.Failure(NotFound);
        }

        var listing = await listings.GetListingDetailsAsync(deal.ListingId, cancellationToken)
            .ConfigureAwait(false);
        if (!DealLeaseSigning.IsRequired(listing))
        {
            return Result<DealLeaseSigningStatusDto>.Failure(NotRequired);
        }

        var existing = await signatures.GetAsync(request.DealId, callerRole.Value, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return await StatusAsync(request.DealId, callerRole, listing, cancellationToken)
                .ConfigureAwait(false);
        }

        var document = await pdfs.GetOrGenerateAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);
        if (document is null || string.IsNullOrWhiteSpace(document.ContentHash))
        {
            return Result<DealLeaseSigningStatusDto>.Failure(PdfNotReady);
        }

        var signature = DealLeaseSignature.Create(
            request.DealId,
            callerRole.Value,
            request.CallerUserId,
            typedName,
            document.ContentHash,
            clock.UtcNow);

        await signatures.SaveAsync(signature, cancellationToken).ConfigureAwait(false);

        // Re-render so the typed name and date land on the stored PDF.
        _ = await pdfs.GetOrGenerateAsync(request.DealId, cancellationToken).ConfigureAwait(false);

        return await StatusAsync(request.DealId, callerRole, listing, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Result<DealLeaseSigningStatusDto>> StatusAsync(
        Guid dealId,
        DealLeaseSignerRole? callerRole,
        ListingDetailsDto? listing,
        CancellationToken cancellationToken)
    {
        var rows = await signatures.ListByDealAsync(dealId, cancellationToken).ConfigureAwait(false);
        return Result<DealLeaseSigningStatusDto>.Success(
            DealLeaseSigning.Status(DealLeaseSigning.IsRequired(listing), callerRole, rows));
    }
}
