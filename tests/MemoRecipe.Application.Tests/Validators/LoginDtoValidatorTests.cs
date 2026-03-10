using MemoRecipe.Application.Validators;
using MemoRecipe.Application.DTOs.Auth;
using FluentValidation.TestHelper;

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_EmailIsEmpty()
    {
        var dto = new LoginDto { Email = "", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_EmailIsInvalid()
    {
        var dto = new LoginDto { Email = "pasunemail", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_NotHaveError_When_EmailIsValid()
    {
        var dto = new LoginDto { Email = "alice@example.com", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_PasswordIsEmpty()
    {
        var dto = new LoginDto { Email = "alice@example.com", Password = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_NotHaveError_When_PasswordIsValid()
    {
        var dto = new LoginDto { Email = "alice@example.com", Password = "nimportequoi" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
