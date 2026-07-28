using MemoRecipe.Api.Tests.Helpers;

namespace MemoRecipe.Api.Tests.Auth;

public class AuthControllerLogoutTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerLogoutTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Logout_ClearsAuthCookie()
    {
        // Arrange - authenticate via helper (cookie posed on _client)
        const string email = "logout.test@test.com";
        await TestUserHelper.CreateAndLoginAsync(_factory, _client, email);

        // Precondition sanity check: /me works before logout
        var meBefore = await _client.GetAsync("api/auth/me");
        Assert.Equal(System.Net.HttpStatusCode.OK, meBefore.StatusCode);

        // Act - logout
        var logoutResponse = await _client.PostAsync("api/auth/logout", content: null);

        // Assert HTTP - logout returns 200
        Assert.Equal(System.Net.HttpStatusCode.OK, logoutResponse.StatusCode);

        // Assert cookie cleared - subsequent /me is now Unauthorized
        // (HttpClient removes the authCookie from its cookie container when the server
        // sends Set-Cookie with an expired date, which is what Response.Cookies.Delete does)
        var meAfter = await _client.GetAsync("api/auth/me");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, meAfter.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCookie_StillReturns200()
    {
        // Arrange - fresh anonymous client, no auth
        var anonymousClient = _factory.CreateClient();

        // Act
        var response = await anonymousClient.PostAsync("api/auth/logout", content: null);

        // Assert - endpoint is [AllowAnonymous] and idempotent, no error even without cookie
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
