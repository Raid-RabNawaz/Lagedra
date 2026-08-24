using FluentValidation;
using Lagedra.Infrastructure.External.Geocoding;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using IAuditTrailWriter = Lagedra.SharedKernel.Integration.IAuditTrailWriter;
using IUserLookupService = Lagedra.SharedKernel.Integration.IUserLookupService;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record UpdateListingCommand(
    Guid ListingId,
    Guid CallerUserId,
    PropertyType PropertyType,
    string Title,
    string Description,
    long MonthlyRentCents,
    int Bedrooms,
    decimal Bathrooms,
    int MinStayDays,
    int MaxStayDays,
    long MaxDepositCents,
    int? SquareFootage = null,
    HouseRulesDto? HouseRules = null,
    CancellationPolicyDto? CancellationPolicy = null,
    IReadOnlyList<Guid>? AmenityIds = null,
    IReadOnlyList<Guid>? SafetyDeviceIds = null,
    IReadOnlyList<Guid>? ConsiderationIds = null,
    bool? InstantBookingEnabled = null,
    Uri? VirtualTourUrl = null,
    string? ApproxAddress = null,
    long? DefaultDepositCents = null,
    bool ClearDefaultDeposit = false,
    long? DepositUnverifiedCents = null,
    long? DepositBackgroundVerifiedCents = null,
    long? DepositPartnerGuaranteedCents = null,
    LeaseTermsDto? LeaseTerms = null,
    ListingManagerRole ManagerRole = ListingManagerRole.Owner,
    Guid? HomeOwnerUserId = null,
    string? HomeOwnerEmail = null,
    bool IncludeBrokerClause = false) : IRequest<Result<ListingDetailsDto>>;

public sealed class UpdateListingCommandValidator : AbstractValidator<UpdateListingCommand>
{
    public UpdateListingCommandValidator()
    {
        RuleFor(x => x.MaxDepositCents)
            .GreaterThan(0)
            .WithMessage("Maximum deposit must be positive.");

        RuleFor(x => x.DepositUnverifiedCents)
            .Must((cmd, v) => DepositValidation.IsWithinCap(v, cmd.MaxDepositCents))
            .WithMessage("Unverified deposit must be between 0 and the maximum deposit.");

        RuleFor(x => x.DepositBackgroundVerifiedCents)
            .Must((cmd, v) => DepositValidation.IsWithinCap(v, cmd.MaxDepositCents))
            .WithMessage("Background-verified deposit must be between 0 and the maximum deposit.");

        RuleFor(x => x.DepositPartnerGuaranteedCents)
            .Must((cmd, v) => DepositValidation.IsWithinCap(v, cmd.MaxDepositCents))
            .WithMessage("Partner-guaranteed deposit must be between 0 and the maximum deposit.");

        RuleFor(x => x)
            .Must(x => DepositValidation.IsOrdered(
                x.DepositPartnerGuaranteedCents,
                x.DepositBackgroundVerifiedCents,
                x.DepositUnverifiedCents))
            .WithMessage("Deposits must satisfy partner-guaranteed \u2264 background-verified \u2264 unverified.");

        RuleFor(x => x.LeaseTerms!.RentDueDayOfMonth)
            .InclusiveBetween(1, 28)
            .When(x => x.LeaseTerms is not null)
            .WithMessage("Rent due day must be between 1 and 28.");
    }
}

public sealed class UpdateListingCommandHandler(
    ListingsDbContext dbContext,
    IGeocodingService geocodingService,
    IAuditTrailWriter auditTrail,
    IUserLookupService userLookup)
    : IRequestHandler<UpdateListingCommand, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error Forbidden = new("Listing.Forbidden", "You do not own this listing.");

    public async Task<Result<ListingDetailsDto>> Handle(
        UpdateListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .Include(l => l.Amenities).ThenInclude(a => a.AmenityDefinition)
            .Include(l => l.SafetyDevices).ThenInclude(s => s.SafetyDeviceDefinition)
            .Include(l => l.Considerations).ThenInclude(c => c.ConsiderationDefinition)
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ListingDetailsDto>.Failure(NotFound);
        }

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Result<ListingDetailsDto>.Failure(Forbidden);
        }

        var management = await ListingManagementGuard.ResolveAsync(
            userLookup,
            request.ManagerRole,
            request.HomeOwnerUserId,
            request.HomeOwnerEmail,
            request.CallerUserId,
            cancellationToken).ConfigureAwait(false);
        if (management.IsFailure)
        {
            return Result<ListingDetailsDto>.Failure(management.Error);
        }

        var rentChanged = listing.MonthlyRentCents != request.MonthlyRentCents;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (rentChanged)
        {
            var openRecord = await dbContext.ListingPriceHistory
                .FirstOrDefaultAsync(h => h.ListingId == listing.Id && h.EffectiveTo == null, cancellationToken)
                .ConfigureAwait(false);
            if (openRecord is not null)
            {
                openRecord.Close(today);
            }
        }

        try
        {
            var stayRange = new StayRange(request.MinStayDays, request.MaxStayDays);

            listing.Update(
                request.PropertyType,
                request.Title,
                request.Description,
                request.MonthlyRentCents,
                request.Bedrooms,
                request.Bathrooms,
                stayRange,
                request.MaxDepositCents,
                request.SquareFootage);

            if (request.HouseRules is { } hr)
            {
                listing.SetHouseRules(Domain.ValueObjects.HouseRules.Create(
                    hr.CheckInTime, hr.CheckOutTime, hr.MaxGuests,
                    hr.PetsAllowed, hr.PetsNotes, hr.SmokingAllowed,
                    hr.PartiesAllowed, hr.QuietHoursStart, hr.QuietHoursEnd,
                    hr.LeavingInstructions, hr.AdditionalRules));
            }

            if (request.LeaseTerms is { } lt)
            {
                listing.SetLeaseTerms(Domain.ValueObjects.LeaseTerms.Create(
                    lt.RentDueDayOfMonth,
                    lt.NsfFirstFeeCents,
                    lt.NsfSubsequentFeeCents,
                    lt.LateFeePercent,
                    lt.LateFeeGraceDays,
                    lt.UtilitiesResponsibility,
                    lt.YardMaintenanceByTenant,
                    lt.Furnished,
                    lt.IncludedAppliancesNotes,
                    lt.KeyCount,
                    lt.MailboxKeyCount,
                    lt.KeyReplacementFeeCents,
                    lt.LockoutFeeCents,
                    lt.ParkingSpaceCount,
                    lt.ParkingDescription,
                    lt.ParkingIncludedInRent,
                    lt.MaxGuestConsecutiveDays,
                    lt.RentersInsuranceMinLiabilityCents,
                    lt.EarlyTerminationFeeMonths,
                    lt.BuiltBefore1978,
                    lt.LeadPaintKnowledge,
                    lt.RentCapJustCauseExempt,
                    lt.PaymentMethods));
            }

            if (request.CancellationPolicy is { } cp)
            {
                listing.SetCancellationPolicy(Domain.ValueObjects.CancellationPolicy.Create(
                    cp.Type, cp.FreeCancellationDays,
                    cp.PartialRefundPercent, cp.PartialRefundDays,
                    cp.CustomTerms));
            }

            if (request.AmenityIds is not null)
            {
                listing.SetAmenities(request.AmenityIds);
            }

            if (request.SafetyDeviceIds is not null)
            {
                listing.SetSafetyDevices(request.SafetyDeviceIds);
            }

            if (request.ConsiderationIds is not null)
            {
                listing.SetConsiderations(request.ConsiderationIds);
            }

            if (request.InstantBookingEnabled.HasValue)
            {
                listing.SetInstantBooking(request.InstantBookingEnabled.Value);
            }

            if (request.VirtualTourUrl is not null)
            {
                listing.SetVirtualTourUrl(request.VirtualTourUrl);
            }

            // Phase 16.2: explicit clear takes precedence; otherwise update only when supplied.
            if (request.ClearDefaultDeposit)
            {
                listing.SetDefaultDeposit(null);
            }
            else if (request.DefaultDepositCents.HasValue)
            {
                listing.SetDefaultDeposit(request.DefaultDepositCents);
            }

            // Predetermined per-verification-tier deposits are submitted with the
            // full edit form, so treat the incoming values as authoritative (null
            // clears a tier back to the MaxDepositCents fallback).
            listing.SetVerificationDeposits(
                request.DepositUnverifiedCents,
                request.DepositBackgroundVerifiedCents,
                request.DepositPartnerGuaranteedCents);

            listing.SetManagement(
                management.Value.ManagerRole,
                management.Value.HomeOwnerUserId,
                request.IncludeBrokerClause);

            if (rentChanged)
            {
                var newRecord = ListingPriceHistory.Create(listing.Id, request.MonthlyRentCents, today);
                dbContext.ListingPriceHistory.Add(newRecord);
            }

            if (!string.IsNullOrWhiteSpace(request.ApproxAddress))
            {
                var geocoded = await geocodingService
                    .GeocodeAddressAsync(request.ApproxAddress, cancellationToken)
                    .ConfigureAwait(false);

                if (geocoded is not null)
                {
                    listing.SetApproxLocation(new GeoPoint(geocoded.Latitude, geocoded.Longitude));
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result<ListingDetailsDto>.Failure(
                new Error("Listing.NotEditable", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditTrail.RecordAsync(
            request.CallerUserId,
            "listing.deposits_set",
            "Listing",
            listing.Id.ToString(),
            CreateListingCommandHandler.FormatDepositDetails(listing),
            ct: cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(
            ListingMapper.ToDetails(listing, homeOwner: management.Value.HomeOwner));
    }
}
