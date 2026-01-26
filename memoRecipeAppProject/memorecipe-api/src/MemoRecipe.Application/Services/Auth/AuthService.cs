using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MemoRecipe.Application.DTOs.Auth;
using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Domain.Entities.Users; 
using AutoMapper;

namespace MemoRecipe.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly MemoRecipeDbContext _db;
    private readonly IMapper _mapper;
    private readonly IJwtService _jwtService;


    public AuthService(MemoRecipeDbContext db, IMapper mapper, IJwtService jwtService)
    {
        _db = db;
        _mapper = mapper;
        _jwtService = jwtService;
    }

    public async Task<AuthUserDto?> RegisterAsync(RegisterDto dto)
    {
        // Vérifier email déjà utilisé ?
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return null;

        // Hash du mot de passe
        PasswordHasher.CreateHash(dto.Password, out string hash, out string salt);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Username = dto.Username,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username
        };
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return null;

        if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            return null;

        return _jwtService.GenerateToken(user);
    }

   public async Task<AuthUserDto?> GetCurrentUserAsync(ClaimsPrincipal user)
    {
        var userId = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return null;

        var userGuid = Guid.Parse(userId);

        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
        if (entity == null)
            return null;

        return new AuthUserDto
        {
            Id = entity.Id,
            Email = entity.Email,
            Username = entity.Username,
        };
    }
}
