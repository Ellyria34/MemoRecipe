using MemoRecipe.Api.Tests.Helpers;

namespace MemoRecipe.Api.Tests.Auth;

public class AuthControllerMeTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerMeTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Me_WithValidCookie_ReturnsUserDto()
    {
        // Arrange - authenticate to set the authCookie on _client
        const string email = "me.valid@test.com";
        await TestUserHelper.CreateAndLoginAsync(_factory, _client, email);

        // Act
        var response = await _client.GetAsync("api/auth/me");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(email, body);
        
        // Regression guard: Me endpoint's UserDto must not expose password-related fields
        Assert.DoesNotContain("PasswordHash", body);
        Assert.DoesNotContain("PasswordSalt", body);

    }

    [Fact]
    public async Task Me_WithoutCookie_Returns401()
    {
        // Arrange - fresh HttpClient without any auth cookie
        var anonymousClient = _factory.CreateClient();

        // Act
        var response = await anonymousClient.GetAsync("api/auth/me");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
