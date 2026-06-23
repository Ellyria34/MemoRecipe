using MemoRecipe.Domain.Entities.Users;

namespace MemoRecipe.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    void Update (User user);
    void Delete(User user);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
