using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.UnitTests.Entities;

public class DiscountTests
{
    private static DateOnly D(int day) => new(2026, 1, day);

    [Fact]
    public void Create_Percentage_SetsProperties()
    {
        var discount = Discount.Create(1, "Autumn Deal", DiscountType.Percentage, 20m, D(10), D(20));

        discount.RoomId.Should().Be(1);
        discount.Name.Should().Be("Autumn Deal");
        discount.Type.Should().Be(DiscountType.Percentage);
        discount.Value.Should().Be(20m);
        discount.StartDate.Should().Be(D(10));
        discount.EndDate.Should().Be(D(20));
        discount.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(DiscountType.Percentage, 0)]
    [InlineData(DiscountType.FixedAmount, -5)]
    public void Create_NonPositiveValue_Throws(DiscountType type, decimal value)
    {
        var act = () => Discount.Create(1, "Deal", type, value, D(10), D(20));

        act.Should().Throw<InvalidDiscountException>();
    }

    [Fact]
    public void Create_PercentageOver100_Throws()
    {
        var act = () => Discount.Create(1, "Deal", DiscountType.Percentage, 150m, D(10), D(20));

        act.Should().Throw<InvalidDiscountException>();
    }

    [Fact]
    public void Create_EndNotAfterStart_Throws()
    {
        var act = () => Discount.Create(1, "Deal", DiscountType.Percentage, 10m, D(20), D(20));

        act.Should().Throw<InvalidDateRangeException>();
    }

    [Theory]
    [InlineData(10)] // start boundary (inclusive)
    [InlineData(15)] // middle
    [InlineData(20)] // end boundary (inclusive)
    public void IsActiveOn_WithinInclusiveRange_ReturnsTrue(int day)
    {
        var discount = Discount.Create(1, "Deal", DiscountType.Percentage, 10m, D(10), D(20));

        discount.IsActiveOn(D(day)).Should().BeTrue();
    }

    [Theory]
    [InlineData(9)]
    [InlineData(21)]
    public void IsActiveOn_OutsideRange_ReturnsFalse(int day)
    {
        var discount = Discount.Create(1, "Deal", DiscountType.Percentage, 10m, D(10), D(20));

        discount.IsActiveOn(D(day)).Should().BeFalse();
    }

    [Fact]
    public void IsActiveOn_AfterDeactivate_ReturnsFalse()
    {
        var discount = Discount.Create(1, "Deal", DiscountType.Percentage, 10m, D(10), D(20));

        discount.Deactivate();

        discount.IsActiveOn(D(15)).Should().BeFalse();
    }

    [Fact]
    public void ApplyTo_Percentage_ReducesByPercentage()
    {
        var discount = Discount.Create(1, "Deal", DiscountType.Percentage, 20m, D(10), D(20));

        discount.ApplyTo(Money.Of(100m)).Should().Be(Money.Of(80m));
    }

    [Fact]
    public void ApplyTo_FixedAmount_SubtractsValue()
    {
        var discount = Discount.Create(1, "Deal", DiscountType.FixedAmount, 30m, D(10), D(20));

        discount.ApplyTo(Money.Of(100m)).Should().Be(Money.Of(70m));
    }

    [Fact]
    public void ApplyTo_FixedAmount_ExceedingPrice_ClampsToZero()
    {
        var discount = Discount.Create(1, "Deal", DiscountType.FixedAmount, 80m, D(10), D(20));

        discount.ApplyTo(Money.Of(50m)).Should().Be(Money.Of(0m));
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var discount = Discount.Create(1, "Deal", DiscountType.Percentage, 10m, D(10), D(20));

        discount.Deactivate();

        discount.IsActive.Should().BeFalse();
    }
}