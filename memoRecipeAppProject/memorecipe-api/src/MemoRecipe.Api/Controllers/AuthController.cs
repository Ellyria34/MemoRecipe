using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Application.DTOs.Auth;
namespace MemoRecipe.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;

    public AuthController(IAuthService authService, IJwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = await _authService.RegisterAsync(dto);

        if (user == null)
            return BadRequest("Email already exists.");

        return Ok(user);
    }

    // LOGIN - retourne un token
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _authService.LoginAsync(dto.Email, dto.Password);

        if (token == null)
            return Unauthorized(new { message = "Invalid email or password" });

        return Ok(new { token });
    }


    // GET/auth/user → if token is present and valid, returns user info
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _authService.GetCurrentUserAsync(User);
        if (user == null)
            return Unauthorized();

        return Ok(user);
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
