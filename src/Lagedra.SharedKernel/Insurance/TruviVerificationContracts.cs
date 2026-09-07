using System.Text.Json.Serialization;

namespace Lagedra.SharedKernel.Insurance;

public sealed record TruviVerificationMetadata(
    [property: JsonPropertyName("timeStamp")] string TimeStamp,
    [property: JsonPropertyName("echoToken")] string EchoToken);

public sealed record TruviCompany(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email);

public sealed record TruviListingAddress(
    [property: JsonPropertyName("addressLine1")] string AddressLine1,
    [property: JsonPropertyName("town")] string Town,
    [property: JsonPropertyName("countryIso")] string CountryIso,
    [property: JsonPropertyName("postcode")] string Postcode,
    [property: JsonPropertyName("addressLine2")] string? AddressLine2 = null);

public sealed record TruviListing(
    [property: JsonPropertyName("address")] TruviListingAddress Address,
    [property: JsonPropertyName("petsAllowed")] bool PetsAllowed,
    [property: JsonPropertyName("numberOfGuests")] int? NumberOfGuests = null,
    [property: JsonPropertyName("numberOfBathrooms")] int? NumberOfBathrooms = null,
    [property: JsonPropertyName("numberOfBedrooms")] int? NumberOfBedrooms = null);

public sealed record TruviReservation(
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("checkIn")] string CheckIn,
    [property: JsonPropertyName("checkOut")] string CheckOut,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("creationDate")] string CreationDate);

public sealed record TruviGuest(
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("phone")] string? Phone = null);

public sealed record TruviProtection(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("extendedAmount")] int ExtendedAmount,
    [property: JsonPropertyName("hasPetProtection")] bool HasPetProtection,
    [property: JsonPropertyName("startingLevel")] int? StartingLevel = null);

public sealed record TruviModifyVerificationRequest(
    [property: JsonPropertyName("metadata")] TruviVerificationMetadata Metadata,
    [property: JsonPropertyName("verification")] TruviCancelVerificationRef Verification,
    [property: JsonPropertyName("reservation")] TruviModifyReservation Reservation,
    [property: JsonPropertyName("listing")] TruviModifyListing? Listing = null);

public sealed record TruviModifyReservation(
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("checkIn")] string CheckIn,
    [property: JsonPropertyName("checkOut")] string CheckOut);

public sealed record TruviModifyListing(
    [property: JsonPropertyName("petsAllowed")] bool PetsAllowed);

public sealed record TruviCreateVerificationRequest(
    [property: JsonPropertyName("metadata")] TruviVerificationMetadata Metadata,
    [property: JsonPropertyName("company")] TruviCompany Company,
    [property: JsonPropertyName("listing")] TruviListing Listing,
    [property: JsonPropertyName("reservation")] TruviReservation Reservation,
    [property: JsonPropertyName("guest")] TruviGuest Guest,
    [property: JsonPropertyName("protection")] TruviProtection Protection);

public sealed record TruviCancelVerificationRequest(
    [property: JsonPropertyName("metadata")] TruviVerificationMetadata Metadata,
    [property: JsonPropertyName("verification")] TruviCancelVerificationRef Verification,
    [property: JsonPropertyName("reservation")] TruviCancelReservationRef Reservation);

public sealed record TruviCancelVerificationRef(
    [property: JsonPropertyName("verificationId")] string VerificationId);

public sealed record TruviCancelReservationRef(
    [property: JsonPropertyName("reservationId")] string ReservationId);

public sealed record TruviVerificationResult(
    string VerificationId,
    string Status,
    string? FlaggedReason);
