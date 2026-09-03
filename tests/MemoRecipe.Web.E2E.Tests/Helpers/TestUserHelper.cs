using System.Net.Http.Json;

namespace MemoRecipe.Web.E2E.Tests.Helpers;

/// <summary>
/// Helper for E2E tests: seeds test users via the API directly (HTTP POST /api/auth/register),
/// bypassing the UI register flow. Faster and more robust than clicking through the register page
/// for every scenario that just needs a valid user to login with.
/// </summary>
public static class TestUserHelper
{
    private const string ApiBaseUrl = "http://localhost:8080";
    private const string RegisterEndpoint = "/api/auth/register";

    /// <summary>
    /// Registers a new user via HTTP. Throws if the API call fails (non-2xx status).
    /// </summary>
    public static async Task CreateUserViaHttpAsync(string email, string userName, string password)
    {
        using var http = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
        var payload = new { email, userName, password };
        var response = await http.PostAsJsonAsync(RegisterEndpoint, payload);
        response.EnsureSuccessStatusCode();
    }
}