using Application.Features.Hotels.Queries.SearchHotels;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Hotels.Queries.SearchHotels;

public class SearchHotelsQueryValidatorTests
{
    private readonly SearchHotelsQueryValidator _validator = new();

    private static SearchHotelsQuery Valid() => new(Keyword: null, CityId: null, CheckIn: null, CheckOut: null);

    [Fact]
    public void ValidQuery_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void PageSizeOutOfRange_HasError(int pageSize)
    {
        _validator.TestValidate(Valid() with { PageSize = pageSize })
            .ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void NonPositiveAdults_HasError()
    {
        _validator.TestValidate(Valid() with { Adults = 0 })
            .ShouldHaveValidationErrorFor(x => x.Adults);
    }

    [Fact]
    public void NonPositivePageNumber_HasError()
    {
        _validator.TestValidate(Valid() with { PageNumber = 0 })
            .ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public void CheckOutNotAfterCheckIn_HasError()
    {
        var day = new DateOnly(2026, 1, 10);
        _validator.TestValidate(Valid() with { CheckIn = day, CheckOut = day })
            .ShouldHaveValidationErrorFor(x => x.CheckOut);
    }

    [Fact]
    public void MaxPriceBelowMinPrice_HasError()
    {
        _validator.TestValidate(Valid() with { MinPrice = 200m, MaxPrice = 100m })
            .ShouldHaveValidationErrorFor(x => x.MaxPrice);
    }
}