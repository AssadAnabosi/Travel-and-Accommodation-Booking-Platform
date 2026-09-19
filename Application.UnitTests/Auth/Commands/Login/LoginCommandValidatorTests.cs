using Application.Features.Auth.Commands.Login;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Auth.Commands.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new LoginCommand("ada@example.com", "secret"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void InvalidEmail_HasError(string email)
    {
        _validator.TestValidate(new LoginCommand(email, "secret"))
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmptyPassword_HasError()
    {
        _validator.TestValidate(new LoginCommand("ada@example.com", ""))
            .ShouldHaveValidationErrorFor(x => x.Password);
    }
}