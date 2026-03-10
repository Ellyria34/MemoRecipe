using MemoRecipe.Application.Validators;
using MemoRecipe.Application.DTOs.Auth;
using FluentValidation.TestHelper;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_EmailIsEmpty()
    {
        var dto = new RegisterDto { Email = "", Username = "Alice", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_EmailIsInvalid()
    {
        var dto = new RegisterDto { Email = "pasunemail", Username = "Alice", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_NotHaveError_When_EmailIsValid()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = "Alice", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_HaveError_When_UsernameIsEmpty()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = "", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_HaveError_When_UsernameIsTooShort()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = "AB", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_HaveError_When_UsernameIsTooLong()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = new string('A', 51), Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_NotHaveError_When_UsernameIsValid()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = "Alice", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_HaveError_When_PasswordIsEmpty()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = "Alice", Password = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordIsTooShort()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = "Alice", Password = "Ab1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_HaveError_When_PasswordHasNoUppercase()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = "Alice", Password = "password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_NotHaveError_When_PasswordIsValid()
    {
        var dto = new RegisterDto { Email = "alice@example.com", Username = "Alice", Password = "Password1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
