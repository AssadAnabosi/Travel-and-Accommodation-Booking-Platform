using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;

namespace Domain.UnitTests.Entities;

public class HotelTests
{
    private static Hotel OwnerHotel() =>
        Hotel.CreateByOwner("Grand", 4, "A nice hotel", "1 Main St", 40.0, -73.0, cityId: 1, ownerId: Guid.NewGuid());

    private static Hotel AdminHotel() =>
        Hotel.CreateByAdmin("Grand", 4, "A nice hotel", "1 Main St", 40.0, -73.0, cityId: 1, ownerId: Guid.NewGuid());

    [Fact]
    public void CreateByOwner_StartsPending_AndNotPubliclyVisible()
    {
        var hotel = OwnerHotel();

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Pending);
        hotel.IsPubliclyVisible.Should().BeFalse();
    }

    [Fact]
    public void CreateByAdmin_StartsApproved_AndPubliclyVisible()
    {
        var hotel = AdminHotel();

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Approved);
        hotel.IsPubliclyVisible.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void CreateByOwner_InvalidStarRating_Throws(int stars)
    {
        var act = () => Hotel.CreateByOwner("Grand", stars, "d", "1 Main St", 40.0, -73.0, 1, Guid.NewGuid());

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-91.0, 0.0)]
    [InlineData(91.0, 0.0)]
    [InlineData(0.0, -181.0)]
    [InlineData(0.0, 181.0)]
    public void CreateByOwner_InvalidCoordinates_Throws(double latitude, double longitude)
    {
        var act = () => Hotel.CreateByOwner("Grand", 4, "d", "1 Main St", latitude, longitude, 1, Guid.NewGuid());

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Approve_PendingHotel_BecomesApproved()
    {
        var hotel = OwnerHotel();

        hotel.Approve();

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Approved);
    }

    [Fact]
    public void Approve_AlreadyApproved_Throws()
    {
        var hotel = AdminHotel();

        var act = () => hotel.Approve();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Reject_SetsStatusAndReason()
    {
        var hotel = OwnerHotel();

        hotel.Reject("Incomplete details");

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Rejected);
        hotel.RejectionReason.Should().Be("Incomplete details");
    }

    [Fact]
    public void Reject_BlankReason_Throws()
    {
        var hotel = OwnerHotel();

        var act = () => hotel.Reject("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resubmit_RejectedHotel_ReturnsToPending()
    {
        var hotel = OwnerHotel();
        hotel.Reject("nope");

        hotel.Resubmit();

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Pending);
    }

    [Fact]
    public void Resubmit_WhenNotRejected_Throws()
    {
        var hotel = OwnerHotel();

        var act = () => hotel.Resubmit();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Approve_AfterReject_ClearsRejectionReason()
    {
        var hotel = OwnerHotel();
        hotel.Reject("nope");

        hotel.Approve();

        hotel.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void AddImage_AssignsIncrementingDisplayOrder()
    {
        var hotel = OwnerHotel();

        var first = hotel.AddImage("u1");
        var second = hotel.AddImage("u2");

        first.DisplayOrder.Should().Be(0);
        second.DisplayOrder.Should().Be(1);
        hotel.Images.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveImage_UnknownId_Throws()
    {
        var hotel = OwnerHotel();

        var act = () => hotel.RemoveImage(999);

        act.Should().Throw<ImageNotFoundException>();
    }

    [Fact]
    public void SetAmenities_DeduplicatesIds()
    {
        var hotel = OwnerHotel();

        hotel.SetAmenities([1, 1, 2]);

        hotel.HotelAmenities.Select(ha => ha.AmenityId).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public void AverageRating_NoReviews_ReturnsZero()
    {
        OwnerHotel().AverageRating().Should().Be(0);
    }
}