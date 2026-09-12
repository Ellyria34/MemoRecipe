using MemoRecipe.Application.Configuration;


namespace MemoRecipe.Application.Tests.Configuration;

public class FeatureFlagsOptionsTests
{
    

    [Fact]
    public void RegistrationEnabled_WhenRegistrationIsNotConfigured_ReturnsFalse()
    {
        // Arrange
        var options = new FeatureFlagsOptions();

        // Act
        var result = options.RegistrationEnabled;

        // Assert
        Assert.False(result);
    }
}
