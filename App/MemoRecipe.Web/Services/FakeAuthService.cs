namespace MemoRecipe.Web.Services;
public class FakeAuthService : IAuthService
{
    private bool _isAuthenticated  = false;
    public Task<bool> LoginAsync(string email, string password)
    {
        if(email == "test@test.com" && password == "Test1234")
        {
            _isAuthenticated = true;
            return Task.FromResult(true);
        }
        else return Task.FromResult(false);
    }

    public Task<bool> RegisterAsync(string email , string userName, string password)
    {
        if(string.IsNullOrEmpty(email)|| string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            return Task.FromResult(false);
        }
        _isAuthenticated = true;
        return Task.FromResult(true);
    }
    public Task LogoutAsync()
    {            
        _isAuthenticated = false;
        return Task.CompletedTask;
    }
    public Task <string?> GetTokenAsync()
    {
        return Task.FromResult<string?>("faketoken");    
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(_isAuthenticated);
    }
}