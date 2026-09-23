using Application.Common.Interfaces.Persistence;
using Application.Features.Hotels.Commands.ReassignHotelOwner;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.TestHelper;
using Moq;

namespace Application.UnitTests.Hotels.Commands.ReassignHotelOwner;

public class ReassignHotelOwnerCommandValidatorTests
{
    private readonly Mock<IUserRepository> _users = new();

    private ReassignHotelOwnerCommandValidator CreateValidator() => new(_users.Object);

    private User GivenUser(UserRole role, bool active = true)
    {
        var user = User.Create("u@example.com", "hash", "U", "Ser", role);
        if (!active) user.Deactivate();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task ActiveHotelOwner_IsValid()
    {
        var owner = GivenUser(UserRole.HotelOwner);

        var result = await CreateValidator().TestValidateAsync(new ReassignHotelOwnerCommand(1, owner.Id));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Admin)]
    public async Task UserWithoutHotelOwnerRole_HasError(UserRole role)
    {
        var user = GivenUser(role);

        var result = await CreateValidator().TestValidateAsync(new ReassignHotelOwnerCommand(1, user.Id));

        result.ShouldHaveValidationErrorFor(x => x.NewOwnerId);
    }

    [Fact]
    public async Task DeactivatedHotelOwner_HasError()
    {
        var owner = GivenUser(UserRole.HotelOwner, active: false);

        var result = await CreateValidator().TestValidateAsync(new ReassignHotelOwnerCommand(1, owner.Id));

        result.ShouldHaveValidationErrorFor(x => x.NewOwnerId);
    }

    [Fact]
    public async Task UnknownUser_HasError()
    {
        var result = await CreateValidator().TestValidateAsync(new ReassignHotelOwnerCommand(1, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(x => x.NewOwnerId);
    }

    [Fact]
    public async Task EmptyOwnerIdOrBadHotelId_HasErrors()
    {
        var result = await CreateValidator().TestValidateAsync(new ReassignHotelOwnerCommand(0, Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.HotelId);
        result.ShouldHaveValidationErrorFor(x => x.NewOwnerId);
    }
}
