using System.Security.Cryptography;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

public sealed record GenerateDealLeasePdfCommand(Guid DealId, Guid? SnapshotId = null)
    : IRequest<Result<DealLeaseDocument>>;

public sealed class GenerateDealLeasePdfCommandHandler(
    ILeaseAgreementFiller filler,
    ILeasePdfGenerator pdfGenerator,
    IDealLeaseDocumentStore documentStore,
    IClock clock) : IRequestHandler<GenerateDealLeasePdfCommand, Result<DealLeaseDocument>>
{
    public async Task<Result<DealLeaseDocument>> Handle(
        GenerateDealLeasePdfCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await documentStore.GetByDealIdAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var filled = await filler.FillForDealAsync(request.DealId, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null && existing.TemplateVersionId == filled.TemplateVersionId)
            {
                return Result<DealLeaseDocument>.Success(existing);
            }

            if (filled.MissingRequiredPlaceholders.Count > 0)
            {
                return Result<DealLeaseDocument>.Failure(new Error(
                    "LeaseTemplate.MissingFields",
                    FormatMissingFieldsMessage(filled.MissingRequiredPlaceholders)));
            }

            var pdf = pdfGenerator.Generate(filled.Title, filled.FilledHtml);
            var hash = Convert.ToHexString(SHA256.HashData(pdf));
            var doc = new DealLeaseDocument(
                request.DealId,
                request.SnapshotId,
                filled.TemplateId,
                filled.TemplateVersionId,
                $"lease-{filled.JurisdictionCode}-deal-{request.DealId:N}.pdf",
                "application/pdf",
                pdf,
                hash,
                clock.UtcNow);

            await documentStore.SaveAsync(doc, cancellationToken).ConfigureAwait(false);
            return Result<DealLeaseDocument>.Success(doc);
        }
        catch (InvalidOperationException ex)
        {
            return Result<DealLeaseDocument>.Failure(new Error("LeaseTemplate.FillFailed", ex.Message));
        }
    }

    private static string FormatMissingFieldsMessage(IReadOnlyList<string> missingKeys)
    {
        var labels = missingKeys
            .Select(key =>
            {
                var def = LeasePlaceholderCatalog.All
                    .FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
                return def is null ? key : def.Label;
            })
            .ToList();

        var list = string.Join(", ", labels);
        if (missingKeys.Any(k => string.Equals(k, "listing.fullAddress", StringComparison.OrdinalIgnoreCase)))
        {
            return "Cannot generate the lease PDF because the listing has no locked property address. "
                + "Open the listing, lock the full street address, then try downloading again. "
                + $"Missing: {list}.";
        }

        return $"Cannot generate the lease PDF — missing required information: {list}.";
    }
}
