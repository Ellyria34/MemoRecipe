using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoRecipe.Application.Services.Recipes;
using MemoRecipe.Application.DTOs.Recipes;

namespace MemoRecipe.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService ;

    public RecipeController(IRecipeService recipeService)
    {
        _recipeService  = recipeService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecipeById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var recipe = await _recipeService.GetByIdAsync(id, userId);
        if (recipe == null)
        {
            return NotFound();
        }
        return Ok(recipe);
    }

    [HttpGet]
    public async Task<IActionResult> GetRecipeByUser()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var recipes = await _recipeService.GetAllByUserAsync(userId);
        return Ok(recipes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipe(RecipeCreateDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var recipeDto = await _recipeService.CreateAsync(dto, userId);

        return CreatedAtAction(nameof(GetRecipeById), new { id = recipeDto.Id }, recipeDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecipe(Guid id, RecipeUpdateDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var recipeDto = await _recipeService.UpdateAsync(id, dto, userId);
        if (recipeDto == null)
        {
            return NotFound();
        }
        return Ok(recipeDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecipe(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _recipeService.DeleteAsync(id, userId);
        if (result == false)
        {
            return NotFound();
        }
        return NoContent();
    }
}