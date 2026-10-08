using Application.Features.Hotels.Queries.GetHotels;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Hotels.Queries.GetHotels;

public class GetHotelsQueryValidatorTests
{
    private readonly GetHotelsQueryValidator _validator = new();

    private static GetHotelsQuery Valid() => new(Keyword: null, CityId: null, ApprovalStatus: null, OwnerId: null);

    [Fact]
    public void ValidQuery_HasNoErrors()
    {
        _validator.TestValidate(Valid() with { Keyword = "Grand", CityId = 1, ApprovalStatus = HotelApprovalStatus.Rejected })
            .ShouldNotHaveAnyValidationErrors();
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
    public void NonPositivePageNumber_HasError()
    {
        _validator.TestValidate(Valid() with { PageNumber = 0 })
            .ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public void NonPositiveCityId_HasError()
    {
        _validator.TestValidate(Valid() with { CityId = 0 })
            .ShouldHaveValidationErrorFor(x => x.CityId);
    }

    [Fact]
    public void UndefinedApprovalStatus_HasError()
    {
        _validator.TestValidate(Valid() with { ApprovalStatus = (HotelApprovalStatus)99 })
            .ShouldHaveValidationErrorFor(x => x.ApprovalStatus);
    }
}
