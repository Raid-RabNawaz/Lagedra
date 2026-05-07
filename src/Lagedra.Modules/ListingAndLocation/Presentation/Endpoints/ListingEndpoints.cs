using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Application.Queries;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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

        group.MapPost("/", CreateListing).RequireAuthorization("RequireMember");
        group.MapGet("/mine", GetMyListings).RequireAuthorization("RequireMember");
        group.MapPut("/{listingId:guid}", UpdateListing).RequireAuthorization("RequireMember");
        group.MapPost("/{listingId:guid}/publish", PublishListing).RequireAuthorization("RequireMember");
        group.MapPost("/{listingId:guid}/close", CloseListing).RequireAuthorization("RequireMember");
        group.MapGet("/{listingId:guid}", GetListingDetails).AllowAnonymous();
        group.MapGet("/", SearchListings).AllowAnonymous();
        group.MapGet("/{listingId:guid}/similar", GetSimilarListings).AllowAnonymous();
        group.MapGet("/{listingId:guid}/share-url", GetShareUrl).AllowAnonymous();
        group.MapGet("/{listingId:guid}/price-history", GetPriceHistory).AllowAnonymous();
        group.MapGet("/{listingId:guid}/availability", GetAvailability).AllowAnonymous();
        group.MapPost("/{listingId:guid}/block-dates", BlockDates).RequireAuthorization("RequireMember");
        group.MapDelete("/{listingId:guid}/block-dates/{blockId:guid}", UnblockDates).RequireAuthorization("RequireMember");

        group.MapPost("/{listingId:guid}/photos", AddPhoto).RequireAuthorization("RequireMember");
        group.MapDelete("/{listingId:guid}/photos/{photoId:guid}", RemovePhoto).RequireAuthorization("RequireMember");
        group.MapPut("/{listingId:guid}/photos/{photoId:guid}/cover", SetCoverPhoto).RequireAuthorization("RequireMember");
        group.MapPut("/{listingId:guid}/photos/reorder", ReorderPhotos).RequireAuthorization("RequireMember");

        group.MapPost("/{listingId:guid}/media/upload", UploadMedia)
            .RequireAuthorization("RequireMember")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(105L * 1024 * 1024));

        var savedGroup = app.MapGroup("/v1/saved-listings")
            .WithTags("Saved Listings")
            .RequireAuthorization();

        savedGroup.MapPost("/{listingId:guid}", SaveListing);
        savedGroup.MapDelete("/{listingId:guid}", UnsaveListing);
        savedGroup.MapGet("/", GetSavedListings);
        savedGroup.MapPost("/{listingId:guid}/collections/{collectionId:guid}", AddListingToCollection);
        savedGroup.MapDelete("/{listingId:guid}/collections", RemoveListingFromCollection);

        var collectionsGroup = app.MapGroup("/v1/saved-listings/collections")
            .WithTags("Saved Listing Collections")
            .RequireAuthorization();

        collectionsGroup.MapPost("/", CreateCollection);
        collectionsGroup.MapGet("/", GetCollections);
        collectionsGroup.MapGet("/{collectionId:guid}", GetCollectionListings);

        return app;
    }

    private static async Task<IResult> GetMyListings(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new GetMyListingsQuery(userId),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CreateListing(
        [FromBody] CreateListingRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new CreateListingCommand(
                userId,
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
                request.ConsiderationIds,
                request.InstantBookingEnabled,
                request.VirtualTourUrl),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/listings/{result.Value.Id}", result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> UpdateListing(
        [FromRoute] Guid listingId,
        [FromBody] UpdateListingRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new UpdateListingCommand(
                listingId,
                userId,
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
                request.ConsiderationIds,
                request.InstantBookingEnabled,
                request.VirtualTourUrl),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> PublishListing(
        [FromRoute] Guid listingId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new PublishListingCommand(listingId, userId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> CloseListing(
        [FromRoute] Guid listingId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new CloseListingCommand(listingId, userId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
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
        [FromQuery] string? keyword,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radiusKm,
        [FromQuery] double? swLat,
        [FromQuery] double? swLng,
        [FromQuery] double? neLat,
        [FromQuery] double? neLng,
        [FromQuery] PropertyType? propertyType,
        [FromQuery] int? minBedrooms,
        [FromQuery] int? minBathrooms,
        [FromQuery] int? minStayDays,
        [FromQuery] int? maxStayDays,
        [FromQuery] long? minPriceCents,
        [FromQuery] long? maxPriceCents,
        [FromQuery] DateOnly? availableFrom,
        [FromQuery] DateOnly? availableTo,
        [FromQuery] Guid[]? amenityIds,
        [FromQuery] Guid[]? safetyDeviceIds,
        [FromQuery] Guid[]? considerationIds,
        [FromQuery] SearchListingsSortBy? sortBy,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SearchListingsQuery(
                keyword,
                latitude, longitude, radiusKm,
                swLat, swLng, neLat, neLng,
                propertyType, minBedrooms, minBathrooms,
                minStayDays, maxStayDays,
                minPriceCents, maxPriceCents,
                availableFrom, availableTo,
                amenityIds,
                safetyDeviceIds,
                considerationIds,
                sortBy ?? SearchListingsSortBy.Newest,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetSimilarListings(
        [FromRoute] Guid listingId,
        [FromQuery] int limit,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetSimilarListingsQuery(listingId, limit <= 0 ? 6 : Math.Min(limit, 20)),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetShareUrl(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetListingShareUrlQuery(listingId),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetPriceHistory(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetListingPriceHistoryQuery(listingId),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
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
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new BlockDatesCommand(listingId, userId, request.CheckInDate, request.CheckOutDate),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/listings/{listingId}/block-dates/{result.Value.Id}", result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> UnblockDates(
        [FromRoute] Guid listingId,
        [FromRoute] Guid blockId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new UnblockDatesCommand(listingId, userId, blockId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> AddPhoto(
        [FromRoute] Guid listingId,
        [FromBody] AddListingPhotoRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new AddListingPhotoCommand(listingId, userId, request.StorageKey, request.Url, request.Caption),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/listings/{listingId}/photos/{result.Value.Id}", result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> RemovePhoto(
        [FromRoute] Guid listingId,
        [FromRoute] Guid photoId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new RemoveListingPhotoCommand(listingId, userId, photoId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> SetCoverPhoto(
        [FromRoute] Guid listingId,
        [FromRoute] Guid photoId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new SetCoverPhotoCommand(listingId, userId, photoId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> UploadMedia(
        [FromRoute] Guid listingId,
        [FromForm] string? caption,
        IFormFile file,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new
            {
                error = "Listing.Media.EmptyFile",
                detail = "No file was uploaded.",
            });
        }

        var userId = GetUserId(httpContext);
        var command = new UploadListingMediaCommand(
            listingId,
            userId,
            file.FileName,
            file.ContentType,
            file.Length,
            caption,
            _ => Task.FromResult(file.OpenReadStream()));

        var result = await mediator.Send(command, cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ReorderPhotos(
        [FromRoute] Guid listingId,
        [FromBody] ReorderPhotosRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new ReorderPhotosCommand(listingId, userId, request.PhotoIdsInOrder),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ToErrorResult(result.Error);
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
        [FromQuery] Guid? collectionId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new GetSavedListingsQuery(userId, collectionId, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> AddListingToCollection(
        [FromRoute] Guid listingId,
        [FromRoute] Guid collectionId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new AddListingToCollectionCommand(userId, listingId, collectionId),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RemoveListingFromCollection(
        [FromRoute] Guid listingId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new RemoveListingFromCollectionCommand(userId, listingId),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CreateCollection(
        [FromBody] CreateCollectionRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new CreateCollectionCommand(userId, request.Name),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/saved-listings/collections/{result.Value.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetCollections(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new GetCollectionsQuery(userId),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetCollectionListings(
        [FromRoute] Guid collectionId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new GetCollectionListingsQuery(userId, collectionId, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize),
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

    private static IResult ToErrorResult(Lagedra.SharedKernel.Results.Error error)
    {
        var payload = new { error = error.Code, detail = error.Description };
        return error.Code switch
        {
            "Listing.Forbidden" => Results.Json(payload, statusCode: StatusCodes.Status403Forbidden),
            "Listing.NotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
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
