using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.Events;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.Aggregates;

public sealed class Listing : AggregateRoot<Guid>
{
    public Guid LandlordUserId { get; private set; }
    public ListingStatus Status { get; private set; }
    public PropertyType PropertyType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public long MonthlyRentCents { get; private set; }
    public bool InsuranceRequired { get; private set; }
    public int Bedrooms { get; private set; }
    public decimal Bathrooms { get; private set; }
    public int? SquareFootage { get; private set; }
    public StayRange? StayRange { get; private set; }
    public GeoPoint? ApproxGeoPoint { get; private set; }
    public Address? PreciseAddress { get; private set; }
    public string? JurisdictionCode { get; private set; }
    public long MaxDepositCents { get; private set; }
    public long? SuggestedDepositLowCents { get; private set; }
    public long? SuggestedDepositHighCents { get; private set; }
    public HouseRules? HouseRules { get; private set; }
    public CancellationPolicy? CancellationPolicy { get; private set; }
    public bool InstantBookingEnabled { get; private set; }
    public Uri? VirtualTourUrl { get; private set; }

    private readonly List<ListingAmenity> _amenities = [];
    private readonly List<ListingSafetyDevice> _safetyDevices = [];
    private readonly List<ListingConsideration> _considerations = [];
    private readonly List<ListingAvailabilityBlock> _availabilityBlocks = [];
    private readonly List<ListingPhoto> _photos = [];

    public IReadOnlyList<ListingAmenity> Amenities => _amenities.AsReadOnly();
    public IReadOnlyList<ListingSafetyDevice> SafetyDevices => _safetyDevices.AsReadOnly();
    public IReadOnlyList<ListingConsideration> Considerations => _considerations.AsReadOnly();
    public IReadOnlyList<ListingAvailabilityBlock> AvailabilityBlocks => _availabilityBlocks.AsReadOnly();
    public IReadOnlyList<ListingPhoto> Photos => _photos.AsReadOnly();

    private Listing() { }

    public static Listing Create(
        Guid landlordUserId,
        PropertyType propertyType,
        string title,
        string description,
        long monthlyRentCents,
        bool insuranceRequired,
        int bedrooms,
        decimal bathrooms,
        StayRange stayRange,
        long maxDepositCents,
        int? squareFootage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(stayRange);

        if (monthlyRentCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyRentCents), "Monthly rent must be positive.");
        }

        if (maxDepositCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepositCents), "Max deposit must be positive.");
        }

        if (bedrooms < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bedrooms), "Bedrooms must be non-negative.");
        }

        if (bathrooms < 0.5m)
        {
            throw new ArgumentOutOfRangeException(nameof(bathrooms), "Bathrooms must be at least 0.5.");
        }

        if (squareFootage is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(squareFootage), "Square footage must be positive.");
        }

        return new Listing
        {
            Id = Guid.NewGuid(),
            LandlordUserId = landlordUserId,
            PropertyType = propertyType,
            Status = ListingStatus.Draft,
            Title = title,
            Description = description,
            MonthlyRentCents = monthlyRentCents,
            InsuranceRequired = insuranceRequired,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            SquareFootage = squareFootage,
            StayRange = stayRange,
            MaxDepositCents = maxDepositCents
        };
    }

    public void Update(
        PropertyType propertyType,
        string title,
        string description,
        long monthlyRentCents,
        bool insuranceRequired,
        int bedrooms,
        decimal bathrooms,
        StayRange stayRange,
        long maxDepositCents,
        int? squareFootage = null)
    {
        EnsureEditable();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(stayRange);

        if (monthlyRentCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyRentCents), "Monthly rent must be positive.");
        }

        if (maxDepositCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepositCents), "Max deposit must be positive.");
        }

        if (bedrooms < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bedrooms), "Bedrooms must be non-negative.");
        }

        if (bathrooms < 0.5m)
        {
            throw new ArgumentOutOfRangeException(nameof(bathrooms), "Bathrooms must be at least 0.5.");
        }

        if (squareFootage is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(squareFootage), "Square footage must be positive.");
        }

        PropertyType = propertyType;
        Title = title;
        Description = description;
        MonthlyRentCents = monthlyRentCents;
        InsuranceRequired = insuranceRequired;
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        SquareFootage = squareFootage;
        StayRange = stayRange;
        MaxDepositCents = maxDepositCents;
    }

    public void SetHouseRules(HouseRules houseRules)
    {
        EnsureEditable();
        ArgumentNullException.ThrowIfNull(houseRules);
        HouseRules = houseRules;
    }

    public void SetCancellationPolicy(CancellationPolicy policy)
    {
        EnsureEditable();
        ArgumentNullException.ThrowIfNull(policy);
        CancellationPolicy = policy;
    }

    public void SetInstantBooking(bool enabled)
    {
        EnsureEditable();
        InstantBookingEnabled = enabled;
    }

    public void SetVirtualTourUrl(Uri? url)
    {
        EnsureEditable();
        if (url is { OriginalString.Length: > 2000 })
        {
            throw new ArgumentOutOfRangeException(nameof(url), "Virtual tour URL must not exceed 2000 characters.");
        }

        VirtualTourUrl = (url is { OriginalString.Length: > 0 and <= 2000 } clean) ? clean : null;
    }

    public void SetAmenities(IEnumerable<Guid> amenityDefinitionIds)
    {
        EnsureEditable();
        ArgumentNullException.ThrowIfNull(amenityDefinitionIds);
        _amenities.Clear();
        foreach (var amenityId in amenityDefinitionIds)
        {
            _amenities.Add(ListingAmenity.Create(Id, amenityId));
        }
    }

    public void SetSafetyDevices(IEnumerable<Guid> safetyDeviceDefinitionIds)
    {
        EnsureEditable();
        ArgumentNullException.ThrowIfNull(safetyDeviceDefinitionIds);
        _safetyDevices.Clear();
        foreach (var deviceId in safetyDeviceDefinitionIds)
        {
            _safetyDevices.Add(ListingSafetyDevice.Create(Id, deviceId));
        }
    }

    public void SetConsiderations(IEnumerable<Guid> considerationDefinitionIds)
    {
        EnsureEditable();
        ArgumentNullException.ThrowIfNull(considerationDefinitionIds);
        _considerations.Clear();
        foreach (var considId in considerationDefinitionIds)
        {
            _considerations.Add(ListingConsideration.Create(Id, considId));
        }
    }

    public ListingPhoto AddPhoto(string storageKey, Uri url, string? caption)
    {
        var isCover = _photos.Count == 0;
        var sortOrder = _photos.Count;
        var photo = ListingPhoto.Create(Id, storageKey, url, caption, isCover, sortOrder);
        _photos.Add(photo);
        return photo;
    }

    public void RemovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId)
            ?? throw new InvalidOperationException("Photo not found.");

        var wasCover = photo.IsCover;
        _photos.Remove(photo);

        if (wasCover && _photos.Count > 0)
        {
            _photos[0].SetCover(true);
        }

        for (var i = 0; i < _photos.Count; i++)
        {
            _photos[i].SetSortOrder(i);
        }
    }

    public void SetCoverPhoto(Guid photoId)
    {
        var target = _photos.FirstOrDefault(p => p.Id == photoId)
            ?? throw new InvalidOperationException("Photo not found.");

        foreach (var photo in _photos)
        {
            photo.SetCover(photo.Id == photoId);
        }
    }

    public void ReorderPhotos(IReadOnlyList<Guid> photoIdsInOrder)
    {
        ArgumentNullException.ThrowIfNull(photoIdsInOrder);

        if (photoIdsInOrder.Count != _photos.Count)
        {
            throw new ArgumentException("Must provide all photo IDs.");
        }

        for (var i = 0; i < photoIdsInOrder.Count; i++)
        {
            var photo = _photos.FirstOrDefault(p => p.Id == photoIdsInOrder[i])
                ?? throw new InvalidOperationException($"Photo {photoIdsInOrder[i]} not found.");
            photo.SetSortOrder(i);
        }
    }

    public void UpdateSuggestedDeposit(long lowCents, long highCents)
    {
        if (lowCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lowCents), "Suggested deposit low must be non-negative.");
        }

        if (highCents < lowCents)
        {
            throw new ArgumentOutOfRangeException(nameof(highCents), "High must be >= low.");
        }

        SuggestedDepositLowCents = lowCents;
        SuggestedDepositHighCents = highCents;
    }

    public void Publish()
    {
        if (Status != ListingStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot publish listing in status '{Status}'.");
        }

        if (ApproxGeoPoint is null)
        {
            throw new InvalidOperationException("Approximate location must be set before publishing.");
        }

        Status = ListingStatus.Published;
        AddDomainEvent(new ListingPublishedEvent(Id, LandlordUserId));
    }

    public void Activate()
    {
        if (Status != ListingStatus.Published)
        {
            throw new InvalidOperationException($"Cannot activate listing in status '{Status}'.");
        }

        if (PreciseAddress is null)
        {
            throw new InvalidOperationException("Precise address must be locked before activation.");
        }

        Status = ListingStatus.Activated;
        AddDomainEvent(new ListingActivatedEvent(Id, LandlordUserId));
    }

    public void Close()
    {
        if (Status is ListingStatus.Closed or ListingStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot close listing in status '{Status}'.");
        }

        Status = ListingStatus.Closed;
    }

    public void SetApproxLocation(GeoPoint geoPoint)
    {
        EnsureEditable();
        ArgumentNullException.ThrowIfNull(geoPoint);
        ApproxGeoPoint = geoPoint;
    }

    public void LockPreciseAddress(Address address, string? jurisdictionCode)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (Status is not (ListingStatus.Draft or ListingStatus.Published))
        {
            throw new InvalidOperationException($"Cannot lock address in status '{Status}'.");
        }

        PreciseAddress = address;
        JurisdictionCode = jurisdictionCode;
        AddDomainEvent(new PreciseAddressLockedEvent(Id, jurisdictionCode));
    }

    private void EnsureEditable()
    {
        if (Status != ListingStatus.Draft)
        {
            throw new InvalidOperationException($"Listing cannot be edited in status '{Status}'.");
        }
    }
}
