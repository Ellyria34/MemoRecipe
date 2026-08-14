using System.Net;
using MemoRecipeIA.Infrastructure.AI;
using Xunit;

namespace MemoRecipe.IA.Tests.Unit.AI;

public class HttpResponseAiExtensionsTests
{
    [Fact]
    public async Task ReadBodyAndEnsureSuccessAsync_WhenSuccess_ReturnsBodyAsString()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\": \"ok\"}")
        };

        // Act
        var body = await response.ReadBodyAndEnsureSuccessAsync("TestProvider");

        // Assert
        Assert.Equal("{\"result\": \"ok\"}", body);
    }

    [Fact]
    public async Task ReadBodyAndEnsureSuccessAsync_WhenFailure_ThrowsWithProviderNameAndStatusCode()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\": \"rate limit\"}")
        };

        // Act + Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => response.ReadBodyAndEnsureSuccessAsync("Groq"));

        Assert.Contains("Groq API error 429", ex.Message);
        Assert.Contains("rate limit", ex.Message);
    }

    [Fact]
    public async Task ReadBodyAndEnsureSuccessAsync_WhenFailureWithEmptyBody_StillThrowsWithStatusCode()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("")
        };

        // Act + Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => response.ReadBodyAndEnsureSuccessAsync("Mistral"));

        Assert.Contains("Mistral API error 500", ex.Message);
    }
}