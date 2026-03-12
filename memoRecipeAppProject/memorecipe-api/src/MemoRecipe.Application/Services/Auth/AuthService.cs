using System.Security.Claims;
using MemoRecipe.Application.DTOs.Auth;
using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Users;
using AutoMapper;

namespace MemoRecipe.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IJwtService _jwtService;

    public AuthService(IUserRepository userRepository, IMapper mapper, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _jwtService = jwtService;
    }

    public async Task<string?> RegisterAsync(RegisterDto dto)
    {
        // Vérifier email déjà utilisé ?
        if (await _userRepository.EmailExistsAsync(dto.Email))
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

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return _jwtService.GenerateToken(user);
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
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

        var entity = await _userRepository.GetByIdAsync(userGuid);
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
