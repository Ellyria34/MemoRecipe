using MemoRecipe.Domain.Entities.Users;

namespace MemoRecipe.Application.Services.Auth;

public interface IJwtService
{
    string GenerateToken(User user);
}
