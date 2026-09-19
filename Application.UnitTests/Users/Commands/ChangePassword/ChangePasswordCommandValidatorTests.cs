using Application.Features.Users.Commands.ChangePassword;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Users.Commands.ChangePassword;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    private static ChangePasswordCommand Valid() => new("OldPass1", "NewPass1");

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCurrentPassword_HasError()
    {
        _validator.TestValidate(Valid() with { CurrentPassword = "" })
            .ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void WeakNewPassword_HasError()
    {
        _validator.TestValidate(Valid() with { NewPassword = "weak" })
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPasswordSameAsCurrent_HasError()
    {
        _validator.TestValidate(new ChangePasswordCommand("SamePass1", "SamePass1"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}