using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Domain.Entities.Users;

namespace MemoRecipe.Application.Tests.Fakes;

public class FakeJwtService : IJwtService
{
    public string GenerateToken(User user) => $"fake-token-{user.Id}";
}
