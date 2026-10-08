using Application.Features.Auth.Commands.Register;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Auth.Commands.Register;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand Valid() => new("ada@example.com", "Password1", "Ada", "Lovelace");

    [Fact]
    public void ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void InvalidEmail_HasError(string email)
    {
        _validator.TestValidate(Valid() with { Email = email })
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("Pass1")]      // too short
    [InlineData("password1")]  // no uppercase
    [InlineData("PASSWORD1")]  // no lowercase
    [InlineData("Password")]   // no digit
    public void WeakPassword_HasError(string password)
    {
        _validator.TestValidate(Valid() with { Password = password })
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void EmptyFirstName_HasError()
    {
        _validator.TestValidate(Valid() with { FirstName = "" })
            .ShouldHaveValidationErrorFor(x => x.FirstName);
    }
}