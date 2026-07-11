using MemoRecipe.Application.Helpers;

namespace MemoRecipe.Application.Tests.Helpers;

public class EmailMaskerTests
{
    [Theory]
    [InlineData("sarah@example.com", "s***@example.com")]
    [InlineData("a@b.com", "a***@b.com")]
    [InlineData("USER@DOMAIN.CO.UK", "U***@DOMAIN.CO.UK")]
    [InlineData("", "***")]
    [InlineData("   ", "***")]
    [InlineData("pasunEmail", "***")]
    [InlineData("@example.com", "***")]
    public void Mask_WithVariousInputs_ReturnsMaskedEmail(string? input, string expected)
    {
        var result = EmailMasker.Mask(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Mask_WithNull_ReturnsFullyMasked()
    {
        var result = EmailMasker.Mask(null);
        Assert.Equal("***", result);
    }
}
