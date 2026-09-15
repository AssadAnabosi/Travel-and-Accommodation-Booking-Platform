using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Hotel : AuditableEntity<int>
{
    public string Name { get; private set; } = null!;
    public int StarRating { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public int CityId { get; private set; }
    public City City { get; private set; } = null!;

    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;

    private readonly List<Room> _rooms = new();
    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    private readonly List<Review> _reviews = new();
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

    private readonly List<HotelAmenity> _hotelAmenities = new();
    public IReadOnlyCollection<HotelAmenity> HotelAmenities => _hotelAmenities.AsReadOnly();

    public HotelApprovalStatus ApprovalStatus { get; private set; }
    public string? RejectionReason { get; private set; }

    public bool IsPubliclyVisible => ApprovalStatus == HotelApprovalStatus.Approved;

    protected Hotel()
    {
    } // EF Core

    private Hotel(string name, int starRating, string description, int cityId, Guid ownerId)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        StarRating = ValidateStarRating(starRating);
        Description = description ?? string.Empty;
        CityId = cityId;
        OwnerId = ownerId;
        CreatedAt = DateTime.UtcNow;
    }

    private Hotel(string name, int starRating, string description, int cityId, Guid ownerId,
        HotelApprovalStatus approvalStatus)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        StarRating = ValidateStarRating(starRating);
        Description = description ?? string.Empty;
        CityId = cityId;
        OwnerId = ownerId;
        ApprovalStatus = approvalStatus;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///  Owner self-service — starts Pending, invisible to public search/featured deals until approved.
    /// </summary>
    public static Hotel CreateByOwner(string name, int starRating, string description, int cityId, Guid ownerId) =>
        new(name, starRating, description, cityId, ownerId, HotelApprovalStatus.Pending);

    /// <summary>
    /// Admin-created — auto-approved, since an Admin creating it is itself the review step.
    /// </summary>
    public static Hotel CreateByAdmin(string name, int starRating, string description, int cityId, Guid ownerId) =>
        new(name, starRating, description, cityId, ownerId, HotelApprovalStatus.Approved);

    public static Hotel Create(string name, int starRating, string description, int cityId, Guid ownerId) =>
        new(name, starRating, description, cityId, ownerId);

    public void Update(string name, int starRating, string description, int cityId)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        StarRating = ValidateStarRating(starRating);
        Description = description ?? string.Empty;
        CityId = cityId;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ReassignOwner(Guid newOwnerId)
    {
        OwnerId = newOwnerId;
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

    public void Reject(string reason)
    {
        ApprovalStatus = HotelApprovalStatus.Rejected;
        RejectionReason = Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));
        ModifiedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (ApprovalStatus == HotelApprovalStatus.Approved)
            throw new InvalidOperationException("Hotel is already approved.");

        ApprovalStatus = HotelApprovalStatus.Approved;
        RejectionReason = null;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Resubmit()
    {
        if (ApprovalStatus != HotelApprovalStatus.Rejected)
            throw new InvalidOperationException("Only a rejected hotel can be resubmitted for review.");

        ApprovalStatus = HotelApprovalStatus.Pending;
        RejectionReason = null;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetAmenities(IEnumerable<int> amenityIds)
    {
        _hotelAmenities.Clear();
        foreach (var amenityId in amenityIds.Distinct())
            _hotelAmenities.Add(HotelAmenity.Create(Id, amenityId));

        ModifiedAt = DateTime.UtcNow;
    }
}