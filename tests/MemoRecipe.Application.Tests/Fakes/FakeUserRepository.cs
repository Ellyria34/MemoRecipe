using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Users;

namespace MemoRecipe.Application.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<User?> GetByIdAsync(Guid id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _users.FirstOrDefault(u => u.Email == email);
        return Task.FromResult(user);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return Task.FromResult(_users.Any(u => u.Email == email));
    }

    public void Update(User user)
    {
        var index = _users.FindIndex(u => u.Id == user.Id);
        if (index >= 0)
            _users[index] = user;
    }

    public void Delete(User user)
    {
        _users.Remove(user);
    }

    public Task AddAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }

    // Test helper to inspect state
    public IReadOnlyList<User> All => _users;
}
