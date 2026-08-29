using MemoRecipe.Application.Helpers;

namespace MemoRecipe.Application.Tests.Helpers;

public class EmailNormalizerTests
{
    [Theory]
    [InlineData("test@example.com", "test@example.com")]
    [InlineData("Test@Example.COM", "test@example.com")]
    [InlineData("USER@DOMAIN.CO.UK", "user@domain.co.uk")]
    [InlineData("  test@example.com  ", "test@example.com")]
    [InlineData("  Test@Example.COM  ", "test@example.com")]
    [InlineData("\tuser@domain.com\n", "user@domain.com")]
    [InlineData("", "")]
    public void Normalize_WithVariousInputs_ReturnsTrimmedLowercase(string input, string expected)
    {
        var result = EmailNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EmailNormalizer.Normalize(null!));
    }

    // Turkish I problem: with a Turkish culture (tr-TR), the default ToLower()
    // converts "I" to dotless "ı" instead of dotted "i". Using ToLowerInvariant()
    // guarantees deterministic lowercasing across all server cultures, preventing
    // email uniqueness bugs when the server culture changes.
    [Fact]
    public void Normalize_WithTurkishI_ReturnsInvariantLowercase()
    {
        var result = EmailNormalizer.Normalize("USER@ISTANBUL.COM");
        Assert.Equal("user@istanbul.com", result);
    }
}