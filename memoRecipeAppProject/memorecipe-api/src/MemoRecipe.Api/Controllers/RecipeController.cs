using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoRecipe.Application.Services.Recipes;
using MemoRecipe.Application.DTOs.Recipes;
using FluentValidation;
using MemoRecipe.Application.Services.OcrScan;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;

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
        // Size verification    
        if(imageFile.Length > 10 * 1024 * 1024)
        {
            return BadRequest("File size exceeds 10 MB limit.");
        }
        
        // Extension verification
        var allowedExtensions = new []{".jpeg", ".jpg", ".png"};
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if(!allowedExtensions.Contains(extension))
        {
            return BadRequest($"Extension {extension} is not allowed. Allowed: .jpg, .jpeg, .png");
        }

        // MIME type verification
        var allowedMimeTypes = new[]{"image/jpeg", "image/png"};
        var mime = imageFile.ContentType;
        if(!allowedMimeTypes.Contains(mime))
        {
            return BadRequest($"MIME Type {mime} is not allowed. Allowed : image/jpeg, image/png");
        }

        //Magic bytes vérification
        using var stream = imageFile.OpenReadStream();
        var magicBytes = new byte[8];
        await stream.ReadExactlyAsync(magicBytes, 0, 8);
        
        if(!IsValidImageMagicBytes(magicBytes))
        {
            return BadRequest("Invalid image file (magic bytes mismatch).");
        }

        stream.Position = 0; // reset the cursor to the beginning for OCR 

        var result = await _ocrScanService.ProcessImageAsync(stream);
        return Ok(result);
    }

    [HttpGet("count")]
    public async Task<IActionResult> CountByUser(Guid id)
    {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var count = await _recipeService.CountByUserAsync(userId);
            return Ok(count);
    }

    private static bool IsValidImageMagicBytes(byte[] magicBytes)
    {
        if(magicBytes == null ||  magicBytes.Length < 8)
        {
            return false;
        }
        byte[] jpegSignature = {0xFF, 0xD8, 0xFF};
        byte[] pngSignature = {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};

        if(magicBytes.Take(3).SequenceEqual(jpegSignature))
        {
            return true;
        }
        if(magicBytes.Take(8).SequenceEqual(pngSignature))
        {
            return true;
        }
        return false;
    }
}
