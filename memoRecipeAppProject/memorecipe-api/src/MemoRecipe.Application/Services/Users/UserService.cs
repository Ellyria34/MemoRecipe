using AutoMapper;
using MemoRecipe.Application.DTOs.Users;
using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using MemoRecipe.Application.Services.Auth;


namespace MemoRecipe.Application.Services.Users;

public class UserService : IUserService
{
    private readonly MemoRecipeDbContext _db;
    private readonly IMapper _mapper;

    public UserService(MemoRecipeDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;

    }
    
    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            throw new Exception("User not found.");

        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email == email);
    }
}
