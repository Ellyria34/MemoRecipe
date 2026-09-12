using MemoRecipeIA.Infrastructure.Configuration;
using Xunit;

namespace MemoRecipe.IA.Tests.Unit.Configuration;

public class SecretResolverTests
{
    [Fact]
    public void Resolve_WhenValueProvided_ReturnsValue()
    {
        // Arrange
        const string inlineValue = "inline-api-key";

        // Act
        var result = SecretResolver.Resolve(inlineValue, "/path/that/does/not/exist");

        // Assert - the inline value wins, the file path is never read
        Assert.Equal(inlineValue, result);
    }

    [Fact]
    public void Resolve_WhenValueEmptyAndFileExists_ReturnsFileContent()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, "file-api-key");

        try
        {
            // Act
            var result = SecretResolver.Resolve(null, filePath);

            // Assert
            Assert.Equal("file-api-key", result);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Resolve_WhenFileEndsWithNewline_ReturnsTrimmedContent()
    {
        // Arrange - most editors append a trailing newline. Without Trim(), the key
        // would be sent as "file-api-key\n" and the provider would answer 401.
        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, "file-api-key\n");

        try
        {
            // Act
            var result = SecretResolver.Resolve(null, filePath);

            // Assert
            Assert.Equal("file-api-key", result);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Resolve_WhenNothingProvided_ReturnsNull()
    {
        // Act
        var result = SecretResolver.Resolve(null, null);

        // Assert
        Assert.Null(result);
    }
}
