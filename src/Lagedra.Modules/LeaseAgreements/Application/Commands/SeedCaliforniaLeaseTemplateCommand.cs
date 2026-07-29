using Lagedra.Modules.LeaseAgreements.Domain.Aggregates;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Application.Commands;

public sealed record SeedCaliforniaLeaseTemplateCommand : IRequest<Result>;

/// <summary>
/// Idempotent seed: creates (or upgrades) a published US-CA lease template so
/// Truth Surface confirmation can generate lease PDFs without a manual
/// dual-approve + publish ceremony for the first jurisdiction pack.
/// Subsequent edits still go through the normal draft → dual-control → publish flow.
/// </summary>
public sealed class SeedCaliforniaLeaseTemplateCommandHandler(LeaseAgreementDbContext db)
    : IRequestHandler<SeedCaliforniaLeaseTemplateCommand, Result>
{
    public async Task<Result> Handle(SeedCaliforniaLeaseTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await db.Templates
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.JurisdictionCode.Code == "US-CA", cancellationToken)
            .ConfigureAwait(false);

        if (template is null)
        {
            template = LeaseAgreementTemplate.CreateDraft("US-CA", "California Residential Lease Agreement");
            var version = template.AddVersion(CaliforniaLeaseBodyHtml);
            version.SetEffectiveDate(DateTime.UtcNow.Date);
            template.PublishSeedVersion(version.Id);
            db.Templates.Add(template);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }

        // Existing env: if nothing is live yet, publish the latest version so
        // bookings don't fail with "No published lease template".
        if (template.ActiveVersionId is null)
        {
            var version = template.Versions
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            if (version is null)
            {
                version = template.AddVersion(CaliforniaLeaseBodyHtml);
                version.SetEffectiveDate(DateTime.UtcNow.Date);
            }
            else if (version.Status == Domain.Enums.LeaseTemplateVersionStatus.Draft)
            {
                if (string.IsNullOrWhiteSpace(version.BodyHtml))
                {
                    version.UpdateDraft(CaliforniaLeaseBodyHtml, DateTime.UtcNow.Date);
                }
                else if (version.EffectiveDate is null)
                {
                    version.SetEffectiveDate(DateTime.UtcNow.Date);
                }
            }

            template.PublishSeedVersion(version.Id);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private const string CaliforniaLeaseBodyHtml =
        """
        <h1>California Lease Agreement</h1>
        <p>This Lease Agreement ("Lease") is entered into on <strong>{{lease.effectiveDate}}</strong>,
        by and between <strong>{{host.fullName}}</strong> ("Landlord"), and
        <strong>{{tenant.fullName}}</strong> ("Tenant").</p>

        <h2>Broker Disclosure</h2>
        <p>If a licensed broker represents the Landlord for this lease, the broker is
        <strong>{{broker.name}}</strong> (DRE License No. <strong>{{broker.dreLicense}}</strong>).
        {{broker.scopeNotes}}</p>

        <h2>Leased Property</h2>
        <p>The Landlord hereby leases to the Tenant the <strong>{{listing.propertyTypeLabel}}</strong>
        located at <strong>{{listing.fullAddress}}</strong> ("Leased Property").</p>

        <h2>Term</h2>
        <p>This Lease will start on <strong>{{deal.startDate}}</strong> ("Start Date") and will continue for a
        fixed term of <strong>{{deal.termMonths}}</strong> months, ending on <strong>{{deal.endDate}}</strong>
        ("End Date").</p>

        <h2>Rent</h2>
        <p>The Tenant agrees to pay rent of <strong>{{deal.monthlyRent}}</strong> due on the
        <strong>{{listing.rentDueDay}}</strong> day of each month. Accepted payment methods:
        <strong>{{listing.paymentMethods}}</strong>. Landlord contact: {{host.phone}} / {{host.email}}.</p>

        <h2>Non-Sufficient Funds</h2>
        <p>The Tenant will be charged <strong>{{listing.nsfFirstFee}}</strong> for the first returned payment and
        <strong>{{listing.nsfSubsequentFee}}</strong> for each subsequent returned payment, in accordance with
        California Civil Code § 1719.</p>

        <h2>Security Deposit</h2>
        <p>Upon execution, the Tenant shall pay a security deposit of <strong>{{deal.securityDeposit}}</strong>
        for the purposes set forth in Civil Code § 1950.5.</p>

        <h2>Late Fee</h2>
        <p>If Rent is not received within <strong>{{listing.lateFeeGraceDays}}</strong> days after the due date,
        Tenant shall pay a late fee equal to <strong>{{listing.lateFeePercent}}</strong> of the monthly Rent.</p>

        <h2>Utilities and Maintenance</h2>
        <p>{{listing.utilitiesResponsibility}}. Yard maintenance by Tenant:
        <strong>{{listing.yardMaintenanceByTenant}}</strong>.</p>

        <h2>Furnishings</h2>
        <p>The Premises is provided as <strong>{{listing.furnished}}</strong>. Included appliances:
        <strong>{{listing.includedAppliances}}</strong>.</p>

        <h2>Keys</h2>
        <p>The Tenant will be given <strong>{{listing.keyCount}}</strong> key(s) and
        <strong>{{listing.mailboxKeyCount}}</strong> mailbox key(s). Key replacement fee:
        <strong>{{listing.keyReplacementFee}}</strong>. Lockout fee: <strong>{{listing.lockoutFee}}</strong>.</p>

        <h2>Parking</h2>
        <p>The Tenant shall be entitled to use <strong>{{listing.parkingSpaces}}</strong> parking space(s):
        <strong>{{listing.parkingDescription}}</strong>.</p>

        <h2>Occupancy</h2>
        <p>Guest count on this booking: <strong>{{deal.guestCount}}</strong>. Maximum guests at one time:
        <strong>{{listing.maxGuests}}</strong>. Guests may not stay more than
        <strong>{{listing.maxGuestConsecutiveDays}}</strong> consecutive days.</p>

        <h2>Pets / Smoking</h2>
        <p>Pets allowed: <strong>{{listing.petsAllowed}}</strong>. {{listing.petsNotes}}
        Smoking allowed: <strong>{{listing.smokingAllowed}}</strong>.</p>

        <h2>Insurance</h2>
        <p>The Tenant shall maintain renter's insurance including personal liability coverage of at least
        <strong>{{listing.rentersInsuranceMinLiability}}</strong>.</p>

        <h2>Early Termination</h2>
        <p>If Tenant elects to terminate early without legal justification, Tenant shall pay an early termination
        fee equal to <strong>{{listing.earlyTerminationFeeMonths}}</strong> months' Rent, or remaining rent until
        re-rented, whichever is less, as permitted by California law.</p>

        <h2>Notices</h2>
        <p>Landlord notice address: <strong>{{host.noticeAddress}}</strong>.
        Tenant notice address: <strong>{{tenant.mailingAddress}}</strong>.
        Tenant phone/email: {{tenant.phone}} / {{tenant.email}}.</p>

        <h2>Lead-Based Paint Disclosure</h2>
        <p>Built before 1978: <strong>{{listing.builtBefore1978}}</strong>.
        {{listing.leadPaintKnowledge}}</p>

        <h2>Mold Notification Addendum</h2>
        <p>The Tenant agrees to maintain the property in a manner that prevents mold or mildew, promptly report
        water intrusion, and allow Landlord entry for inspection and repairs.</p>

        <h2>Rent Cap and Just Cause Addendum</h2>
        <p>California Civil Code §§ 1947.12 and 1946.2 may limit rent increases and require just cause for
        termination. Rent-cap / just-cause exemption claimed: <strong>{{listing.rentCapJustCauseExempt}}</strong>.</p>
        """;
}
