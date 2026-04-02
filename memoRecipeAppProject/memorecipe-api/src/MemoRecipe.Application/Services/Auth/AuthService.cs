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
    private readonly PasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, IMapper mapper, IJwtService jwtService, PasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<string?> RegisterAsync(RegisterDto dto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Username = dto.Username,
            PasswordHash = "",
            PasswordSalt = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        // Vérifier email déjà utilisé ?
        if (await _userRepository.EmailExistsAsync(dto.Email))
            return null;

        // Hash du mot de passe
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return _jwtService.GenerateToken(user);
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return null;

        if (!_passwordHasher.Verify(user, user.PasswordHash, password, user.PasswordSalt))
            return null;

        if (_passwordHasher.NeedsRehash(user.PasswordSalt))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                user.PasswordSalt = "";
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            }

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
