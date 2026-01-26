using Microsoft.AspNetCore.Mvc;
using MemoRecipe.Application.DTOs.Users;
using MemoRecipe.Application.Services.Users;

namespace MemoRecipe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // GET api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user); // Returns UserDto
    }
}
