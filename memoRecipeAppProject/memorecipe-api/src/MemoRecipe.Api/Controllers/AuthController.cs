using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Application.DTOs.Auth;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;


namespace MemoRecipe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly IValidator<RegisterDto> _registerDtoValidator;
    private readonly IValidator<LoginDto> _loginDtoValidator;
    private readonly IWebHostEnvironment _env;


    public AuthController(IAuthService authService, IJwtService jwtService, IValidator<RegisterDto> registerDtoValidator, IValidator<LoginDto> loginDtoValidator, IWebHostEnvironment env)
    {
        _authService = authService;
        _jwtService = jwtService;
        _registerDtoValidator = registerDtoValidator;
        _loginDtoValidator = loginDtoValidator;
        _env = env;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {

        var validation = await _registerDtoValidator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    return BadRequest(validation.Errors);            
                }

        var token = await _authService.RegisterAsync(dto);

        if (token == null)
            return BadRequest("Email already exists.");

        Response.Cookies.Append("authCookie", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), 
            SameSite = SameSiteMode.Strict, 
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Ok();
    }

    // LOGIN - retourne un token
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        
        var validation = await _loginDtoValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors);            
        }
        
        var token = await _authService.LoginAsync(dto.Email, dto.Password);

        if (token == null)
            return Unauthorized(new { message = "Invalid email or password" });

        Response.Cookies.Append("authCookie", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), 
            SameSite = SameSiteMode.Strict, 
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Ok();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("authCookie");
        return Ok();
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
