using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class Hotel : AuditableEntity<int>
{
    public string Name { get; private set; } = null!;
    public int StarRating { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public string Address { get; private set; } = null!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public int CityId { get; private set; }
    public City City { get; private set; } = null!;

    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;

    public HotelApprovalStatus ApprovalStatus { get; private set; }
    public string? RejectionReason { get; private set; }

    public bool IsPubliclyVisible => ApprovalStatus == HotelApprovalStatus.Approved;

    private readonly List<Room> _rooms = new();
    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    private readonly List<Review> _reviews = new();
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

    private readonly List<HotelAmenity> _hotelAmenities = new();
    public IReadOnlyCollection<HotelAmenity> HotelAmenities => _hotelAmenities.AsReadOnly();

    private readonly List<HotelImage> _images = new();
    public IReadOnlyCollection<HotelImage> Images => _images.AsReadOnly();

    protected Hotel()
    {
    } // EF Core

    private Hotel(string name, int starRating, string description, string address, double latitude, double longitude,
        int cityId, Guid ownerId, HotelApprovalStatus approvalStatus)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        StarRating = ValidateStarRating(starRating);
        Description = description ?? string.Empty;
        Address = Guard.AgainstNullOrWhiteSpace(address, nameof(address));
        (Latitude, Longitude) = ValidateCoordinates(latitude, longitude);
        CityId = cityId;
        OwnerId = ownerId;
        ApprovalStatus = approvalStatus;
        CreatedAt = DateTime.UtcNow;
    }

    public static Hotel CreateByOwner(string name, int starRating, string description, string address,
        double latitude, double longitude, int cityId, Guid ownerId) =>
        new(name, starRating, description, address, latitude, longitude, cityId, ownerId, HotelApprovalStatus.Pending);

    public static Hotel CreateByAdmin(string name, int starRating, string description, string address,
        double latitude, double longitude, int cityId, Guid ownerId) =>
        new(name, starRating, description, address, latitude, longitude, cityId, ownerId, HotelApprovalStatus.Approved);

    public void Update(string name, int starRating, string description, string address, double latitude,
        double longitude, int cityId)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        StarRating = ValidateStarRating(starRating);
        Description = description ?? string.Empty;
        Address = Guard.AgainstNullOrWhiteSpace(address, nameof(address));
        (Latitude, Longitude) = ValidateCoordinates(latitude, longitude);
        CityId = cityId;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ReassignOwner(Guid newOwnerId)
    {
        OwnerId = newOwnerId;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetAmenities(IEnumerable<int> amenityIds)
    {
        _hotelAmenities.Clear();
        foreach (var amenityId in amenityIds.Distinct())
            _hotelAmenities.Add(HotelAmenity.Create(Id, amenityId));

        ModifiedAt = DateTime.UtcNow;
    }

    public HotelImage AddImage(string url)
    {
        var nextOrder = _images.Count == 0 ? 0 : _images.Max(i => i.DisplayOrder) + 1;
        var image = HotelImage.Create(Id, url, nextOrder);
        _images.Add(image);
        return image;
    }

    public void RemoveImage(int imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
                    ?? throw new ImageNotFoundException(imageId, "hotel");
        _images.Remove(image);
    }

    public void Approve()
    {
        if (ApprovalStatus == HotelApprovalStatus.Approved)
            throw new InvalidStateTransitionException("Hotel is already approved.");

        ApprovalStatus = HotelApprovalStatus.Approved;
        RejectionReason = null;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        ApprovalStatus = HotelApprovalStatus.Rejected;
        RejectionReason = Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        ModifiedAt = DateTime.UtcNow;
    }

    public void Resubmit()
    {
        if (ApprovalStatus != HotelApprovalStatus.Rejected)
            throw new InvalidStateTransitionException("Only a rejected hotel can be resubmitted for review.");

        ApprovalStatus = HotelApprovalStatus.Pending;
        RejectionReason = null;
        ModifiedAt = DateTime.UtcNow;
    }

    public double AverageRating() =>
        _reviews.Count == 0 ? 0 : _reviews.Average(r => r.Rating);

    private static int ValidateStarRating(int starRating)
    {
        if (starRating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(starRating), "Star rating must be between 1 and 5.");
        return starRating;
    }

    private static (double, double) ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        return (latitude, longitude);
    }
}