using FluentValidation;
using Lagedra.Infrastructure.External.Geocoding;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.Policies;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using CancellationPolicyType = Lagedra.SharedKernel.Integration.CancellationPolicyType;
using IAuditTrailWriter = Lagedra.SharedKernel.Integration.IAuditTrailWriter;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using System.Text.Json;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record CreateListingCommand(
    Guid LandlordUserId,
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
    bool InstantBookingEnabled = false,
    Uri? VirtualTourUrl = null,
    string? ApproxAddress = null,
    long? DefaultDepositCents = null,
    long? DepositUnverifiedCents = null,
    long? DepositBackgroundVerifiedCents = null,
    long? DepositPartnerGuaranteedCents = null) : IRequest<Result<ListingDetailsDto>>;

public sealed class CreateListingCommandValidator : AbstractValidator<CreateListingCommand>
{
    public CreateListingCommandValidator()
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
    }
}

public sealed class CreateListingCommandHandler(
    ListingsDbContext dbContext,
    IGeocodingService geocodingService,
    IAuditTrailWriter auditTrail)
    : IRequestHandler<CreateListingCommand, Result<ListingDetailsDto>>
{
    public async Task<Result<ListingDetailsDto>> Handle(
        CreateListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stayRange = new StayRange(request.MinStayDays, request.MaxStayDays);

        var listing = Listing.Create(
            request.LandlordUserId,
            request.PropertyType,
            request.Title,
            request.Description,
            request.MonthlyRentCents,
            request.Bedrooms,
            request.Bathrooms,
            stayRange,
            request.MaxDepositCents,
            request.SquareFootage);

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

        if (request.HouseRules is { } hr)
        {
            listing.SetHouseRules(Domain.ValueObjects.HouseRules.Create(
                hr.CheckInTime, hr.CheckOutTime, hr.MaxGuests,
                hr.PetsAllowed, hr.PetsNotes, hr.SmokingAllowed,
                hr.PartiesAllowed, hr.QuietHoursStart, hr.QuietHoursEnd,
                hr.LeavingInstructions, hr.AdditionalRules));
        }

        if (request.CancellationPolicy is { } cp)
        {
            listing.SetCancellationPolicy(Domain.ValueObjects.CancellationPolicy.Create(
                cp.Type, cp.FreeCancellationDays,
                cp.PartialRefundPercent, cp.PartialRefundDays,
                cp.CustomTerms));
        }
        else
        {
            listing.SetCancellationPolicy(CancellationPolicyDefaults.ForType(CancellationPolicyType.Moderate));
        }

        if (request.AmenityIds is { Count: > 0 })
        {
            listing.SetAmenities(request.AmenityIds);
        }

        if (request.SafetyDeviceIds is { Count: > 0 })
        {
            listing.SetSafetyDevices(request.SafetyDeviceIds);
        }

        if (request.ConsiderationIds is { Count: > 0 })
        {
            listing.SetConsiderations(request.ConsiderationIds);
        }

        listing.SetInstantBooking(request.InstantBookingEnabled);
        if (request.VirtualTourUrl is not null)
        {
            listing.SetVirtualTourUrl(request.VirtualTourUrl);
        }

        // Phase 16.2: optional default deposit for instant-book quotes.
        if (request.DefaultDepositCents.HasValue)
        {
            listing.SetDefaultDeposit(request.DefaultDepositCents);
        }

        // Predetermined per-verification-tier deposits (drive the new booking
        // flow). Any null falls back to MaxDepositCents at booking time.
        listing.SetVerificationDeposits(
            request.DepositUnverifiedCents,
            request.DepositBackgroundVerifiedCents,
            request.DepositPartnerGuaranteedCents);

        dbContext.Listings.Add(listing);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var initialPrice = ListingPriceHistory.Create(listing.Id, listing.MonthlyRentCents, today);
        dbContext.ListingPriceHistory.Add(initialPrice);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await auditTrail.RecordAsync(
            request.LandlordUserId,
            "listing.deposits_set",
            "Listing",
            listing.Id.ToString(),
            FormatDepositDetails(listing),
            ct: cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }

    internal static string FormatDepositDetails(Listing listing) =>
        JsonSerializer.Serialize(new
        {
            maxDepositCents = listing.MaxDepositCents,
            unverifiedCents = listing.DepositUnverifiedCents,
            backgroundVerifiedCents = listing.DepositBackgroundVerifiedCents,
            partnerGuaranteedCents = listing.DepositPartnerGuaranteedCents,
        });
}
