using FluentValidation.Results;
using MemoRecipe.Application.DTOs.Auth;
using MemoRecipe.Application.Helpers;

namespace MemoRecipe.Application.Tests.Helpers;

public class ValidationErrorSanitizerTests
{
    [Fact]
    public void Sanitize_WithSensitiveProperty_MasksAttemptedValueToNull()
    {
        // Arrange
        var errors = new List<ValidationFailure>
        {
            new("Password", "Password doit contenir une majuscule.")
            {
                AttemptedValue = "monsupermotdepasse"
            }
        };

        // Act
        var result = ValidationErrorSanitizer.Sanitize<RegisterDto>(errors).ToList();

        // Assert
        Assert.Single(result);
        var sanitized = result[0];
        var attemptedValueProp = sanitized.GetType().GetProperty("AttemptedValue");
        Assert.NotNull(attemptedValueProp);
        Assert.Null(attemptedValueProp!.GetValue(sanitized));
    }

    [Fact]
    public void Sanitize_WithNonSensitiveProperty_PreservesAttemptedValue()
    {
        // Arrange
        var errors = new List<ValidationFailure>
        {
            new("Email", "Email n'est pas au format valide.")
            {
                AttemptedValue = "test@gmial"
            }
        };

        // Act
        var result = ValidationErrorSanitizer.Sanitize<RegisterDto>(errors).ToList();

        // Assert
        Assert.Single(result);
        var sanitized = result[0];
        var attemptedValueProp = sanitized.GetType().GetProperty("AttemptedValue");
        Assert.NotNull(attemptedValueProp);
        Assert.Equal("test@gmial", attemptedValueProp!.GetValue(sanitized));
    }

    [Fact]
    public void Sanitize_KeepsPropertyNameAndErrorMessageIntact()
    {
        // Arrange
        var errors = new List<ValidationFailure>
        {
            new("Password", "Password doit contenir une majuscule.")
            {
                AttemptedValue = "abc"
            },
            new("Email", "Email n'est pas au format valide.")
            {
                AttemptedValue = "invalid"
            }
        };

        // Act
        var result = ValidationErrorSanitizer.Sanitize<RegisterDto>(errors).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        // Password error: PropertyName + ErrorMessage kept, AttemptedValue null
        var passwordError = result[0];
        Assert.Equal("Password", passwordError.GetType().GetProperty("PropertyName")!.GetValue(passwordError));
        Assert.Equal("Password doit contenir une majuscule.", passwordError.GetType().GetProperty("ErrorMessage")!.GetValue(passwordError));

        // Email error: everything intact
        var emailError = result[1];
        Assert.Equal("Email", emailError.GetType().GetProperty("PropertyName")!.GetValue(emailError));
        Assert.Equal("Email n'est pas au format valide.", emailError.GetType().GetProperty("ErrorMessage")!.GetValue(emailError));
    }

    [Fact]
    public void Sanitize_WithEmptyErrors_ReturnsEmpty()
    {
        // Arrange
        var errors = new List<ValidationFailure>();

        // Act
        var result = ValidationErrorSanitizer.Sanitize<RegisterDto>(errors);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Sanitize_LoginDto_MasksPasswordButKeepsEmail()
    {
        // Arrange
        var errors = new List<ValidationFailure>
    {
        new("Password", "Password requis.") { AttemptedValue = "leaked_password" },
        new("Email", "Email invalide.") { AttemptedValue = "test@example.com" }
    };

        // Act
        var result = ValidationErrorSanitizer.Sanitize<LoginDto>(errors).ToList();

        // Assert
        var passwordError = result[0];
        Assert.Null(passwordError.GetType().GetProperty("AttemptedValue")!.GetValue(passwordError));

        var emailError = result[1];
        Assert.Equal("test@example.com", emailError.GetType().GetProperty("AttemptedValue")!.GetValue(emailError));
    }

    [Fact]
    public void Sanitize_DeleteAccountDto_MasksPassword()
    {
        // Arrange
        var errors = new List<ValidationFailure>
    {
        new("Password", "Password requis.") { AttemptedValue = "secret_before_delete" }
    };

        // Act
        var result = ValidationErrorSanitizer.Sanitize<DeleteAccountDto>(errors).ToList();

        // Assert
        var passwordError = result[0];
        Assert.Null(passwordError.GetType().GetProperty("AttemptedValue")!.GetValue(passwordError));
    }

    [Fact]
    public void Sanitize_MultipleErrorsOnSensitiveProperty_MasksAllOfThem()
    {
        // Arrange
        var errors = new List<ValidationFailure>
    {
        new("Password", "Password trop court.") { AttemptedValue = "abc" },
        new("Password", "Password doit contenir un chiffre.") { AttemptedValue = "abc" },
        new("Password", "Password doit contenir une majuscule.") { AttemptedValue = "abc" }
    };

        // Act
        var result = ValidationErrorSanitizer.Sanitize<RegisterDto>(errors).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        foreach (var error in result)
        {
            Assert.Null(error.GetType().GetProperty("AttemptedValue")!.GetValue(error));
        }
    }

    [Fact]
    public void Sanitize_WithNullAttemptedValueOnSensitiveProperty_KeepsNull()
    {
        // Arrange
        var errors = new List<ValidationFailure>
    {
        new("Password", "Password requis.") // AttemptedValue non défini = null
    };

        // Act
        var result = ValidationErrorSanitizer.Sanitize<RegisterDto>(errors).ToList();

        // Assert
        var passwordError = result[0];
        Assert.Null(passwordError.GetType().GetProperty("AttemptedValue")!.GetValue(passwordError));
    }
}
