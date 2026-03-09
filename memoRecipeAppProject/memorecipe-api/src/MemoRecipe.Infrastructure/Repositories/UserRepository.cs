using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace MemoRecipe.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MemoRecipeDbContext _db;

    public UserRepository(MemoRecipeDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        await _db.Users.AddAsync(user);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
