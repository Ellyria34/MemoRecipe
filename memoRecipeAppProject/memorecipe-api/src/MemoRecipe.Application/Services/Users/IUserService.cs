using MemoRecipe.Application.DTOs.Users;

namespace MemoRecipe.Application.Services.Users;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<bool> ExistsByEmailAsync(string email);
}
