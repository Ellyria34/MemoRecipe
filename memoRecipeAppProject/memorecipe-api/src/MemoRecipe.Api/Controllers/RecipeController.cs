using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoRecipe.Application.Services.Recipes;
using MemoRecipe.Application.DTOs.Recipes;
using FluentValidation;
using MemoRecipe.Application.Services.OcrScan;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.RateLimiting;

namespace MemoRecipe.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService ;
    private readonly IValidator<RecipeCreateDto> _createDtoValidator;
    private readonly IValidator<RecipeUpdateDto> _updateDtoValidator;
    private readonly IOcrScanService _ocrScanService;

    public RecipeController(
        IRecipeService recipeService, 
        IValidator<RecipeCreateDto> createDtoValidator, 
        IValidator<RecipeUpdateDto> updateDtoValidator,
        IOcrScanService ocrScanService)
    {
        _recipeService = recipeService;
        _createDtoValidator  = createDtoValidator;
        _updateDtoValidator = updateDtoValidator;
        _ocrScanService = ocrScanService;
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
    public async Task<IActionResult> GetRecipeByUser([FromQuery] RecipeQueryParams queryParams)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var recipes = await _recipeService.GetAllByUserAsync(userId, queryParams);
        return Ok(recipes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipe(RecipeCreateDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var validation = await _createDtoValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors);            
        }
        
        var recipeDto = await _recipeService.CreateAsync(dto, userId);

        return CreatedAtAction(nameof(GetRecipeById), new { id = recipeDto.Id }, recipeDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecipe(Guid id, RecipeUpdateDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var validation = await _updateDtoValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors);            
        }

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

    [HttpPost("scan")]
    [EnableRateLimiting("scan")]
    [RequestSizeLimit(10 * 1024 * 1024)]                                //Limit request size
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]    //Limit upload de fichiers
    public async Task<IActionResult> CreateScannedRecipe(IFormFile imageFile)
    {        
        var result = await _ocrScanService.ProcessImageAsync(imageFile.OpenReadStream());
        return Ok(result);
    }

    [HttpGet("count")]
    public async Task<IActionResult> CountByUser(Guid id)
    {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var count = await _recipeService.CountByUserAsync(userId);
            return Ok(count);
    }
}