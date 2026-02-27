using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Application.Queries;
using Lagedra.Modules.ListingAndLocation.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ListingAndLocation.Presentation.Endpoints;

public static class ListingEndpoints
{
    public static IEndpointRouteBuilder MapListingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/listings")
            .WithTags("Listings");

        group.MapPost("/", CreateListing).RequireAuthorization("RequireLandlord");
        group.MapPut("/{listingId:guid}", UpdateListing).RequireAuthorization("RequireLandlord");
        group.MapPost("/{listingId:guid}/publish", PublishListing).RequireAuthorization("RequireLandlord");
        group.MapPost("/{listingId:guid}/close", CloseListing).RequireAuthorization("RequireLandlord");
        group.MapGet("/{listingId:guid}", GetListingDetails).AllowAnonymous();
        group.MapGet("/", SearchListings).AllowAnonymous();
        group.MapGet("/{listingId:guid}/availability", GetAvailability).AllowAnonymous();
        group.MapPost("/{listingId:guid}/block-dates", BlockDates).RequireAuthorization("RequireLandlord");
        group.MapDelete("/{listingId:guid}/block-dates/{blockId:guid}", UnblockDates).RequireAuthorization("RequireLandlord");

        group.MapPost("/{listingId:guid}/photos", AddPhoto).RequireAuthorization("RequireLandlord");
        group.MapDelete("/{listingId:guid}/photos/{photoId:guid}", RemovePhoto).RequireAuthorization("RequireLandlord");
        group.MapPut("/{listingId:guid}/photos/{photoId:guid}/cover", SetCoverPhoto).RequireAuthorization("RequireLandlord");
        group.MapPut("/{listingId:guid}/photos/reorder", ReorderPhotos).RequireAuthorization("RequireLandlord");

        var savedGroup = app.MapGroup("/v1/saved-listings")
            .WithTags("Saved Listings")
            .RequireAuthorization();

        savedGroup.MapPost("/{listingId:guid}", SaveListing);
        savedGroup.MapDelete("/{listingId:guid}", UnsaveListing);
        savedGroup.MapGet("/", GetSavedListings);

        return app;
    }

    private static async Task<IResult> CreateListing(
        [FromBody] CreateListingRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateListingCommand(
                request.LandlordUserId,
                request.PropertyType,
                request.Title,
                request.Description,
                request.MonthlyRentCents,
                request.InsuranceRequired,
                request.Bedrooms,
                request.Bathrooms,
                request.MinStayDays,
                request.MaxStayDays,
                request.MaxDepositCents,
                request.SquareFootage,
                MapHouseRules(request.HouseRules),
                MapCancellationPolicy(request.CancellationPolicy),
                request.AmenityIds,
                request.SafetyDeviceIds,
                request.ConsiderationIds),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/listings/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UpdateListing(
        [FromRoute] Guid listingId,
        [FromBody] UpdateListingRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateListingCommand(
                listingId,
                request.PropertyType,
                request.Title,
                request.Description,
                request.MonthlyRentCents,
                request.InsuranceRequired,
                request.Bedrooms,
                request.Bathrooms,
                request.MinStayDays,
                request.MaxStayDays,
                request.MaxDepositCents,
                request.SquareFootage,
                MapHouseRules(request.HouseRules),
                MapCancellationPolicy(request.CancellationPolicy),
                request.AmenityIds,
                request.SafetyDeviceIds,
                request.ConsiderationIds),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> PublishListing(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new PublishListingCommand(listingId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CloseListing(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CloseListingCommand(listingId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetListingDetails(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetListingDetailsQuery(listingId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> SearchListings(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radiusKm,
        [FromQuery] Domain.Enums.PropertyType? propertyType,
        [FromQuery] int? minBedrooms,
        [FromQuery] int? minBathrooms,
        [FromQuery] int? minStayDays,
        [FromQuery] int? maxStayDays,
        [FromQuery] long? minPriceCents,
        [FromQuery] long? maxPriceCents,
        [FromQuery] DateOnly? availableFrom,
        [FromQuery] DateOnly? availableTo,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SearchListingsQuery(
                latitude, longitude, radiusKm,
                propertyType, minBedrooms, minBathrooms,
                minStayDays, maxStayDays,
                minPriceCents, maxPriceCents,
                availableFrom, availableTo,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetAvailability(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetListingAvailabilityQuery(listingId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> BlockDates(
        [FromRoute] Guid listingId,
        [FromBody] BlockDatesRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new BlockDatesCommand(listingId, request.CheckInDate, request.CheckOutDate),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/listings/{listingId}/block-dates/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UnblockDates(
        [FromRoute] Guid listingId,
        [FromRoute] Guid blockId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UnblockDatesCommand(listingId, blockId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> AddPhoto(
        [FromRoute] Guid listingId,
        [FromBody] AddListingPhotoRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddListingPhotoCommand(listingId, request.StorageKey, request.Url, request.Caption),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/listings/{listingId}/photos/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RemovePhoto(
        [FromRoute] Guid listingId,
        [FromRoute] Guid photoId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RemoveListingPhotoCommand(listingId, photoId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> SetCoverPhoto(
        [FromRoute] Guid listingId,
        [FromRoute] Guid photoId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SetCoverPhotoCommand(listingId, photoId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ReorderPhotos(
        [FromRoute] Guid listingId,
        [FromBody] ReorderPhotosRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ReorderPhotosCommand(listingId, request.PhotoIdsInOrder),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> SaveListing(
        [FromRoute] Guid listingId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new SaveListingCommand(userId, listingId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/saved-listings/{listingId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UnsaveListing(
        [FromRoute] Guid listingId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new UnsaveListingCommand(userId, listingId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetSavedListings(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new GetSavedListingsQuery(userId, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }

    private static HouseRulesDto? MapHouseRules(HouseRulesRequest? request)
    {
        if (request is null)
        {
            return null;
        }

        return new HouseRulesDto(
            request.CheckInTime, request.CheckOutTime, request.MaxGuests,
            request.PetsAllowed, request.PetsNotes, request.SmokingAllowed,
            request.PartiesAllowed, request.QuietHoursStart, request.QuietHoursEnd,
            request.LeavingInstructions, request.AdditionalRules);
    }

    private static CancellationPolicyDto? MapCancellationPolicy(CancellationPolicyRequest? request)
    {
        if (request is null)
        {
            return null;
        }

        return new CancellationPolicyDto(
            request.Type, request.FreeCancellationDays,
            request.PartialRefundPercent, request.PartialRefundDays,
            request.CustomTerms);
    }
}
