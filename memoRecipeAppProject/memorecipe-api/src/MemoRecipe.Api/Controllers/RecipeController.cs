using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoRecipe.Application.Services.Recipes;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.Services.AISecurity;
using FluentValidation;
using MemoRecipe.Application.Services.OcrScan;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MemoRecipe.Application.Configuration;
using System.Diagnostics;

namespace MemoRecipe.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IValidator<RecipeCreateDto> _createDtoValidator;
    private readonly IValidator<RecipeUpdateDto> _updateDtoValidator;
    private readonly IOcrScanService _ocrScanService;
    private readonly FeatureFlagsOptions _flags;
    private readonly IAiRateLimiter _aiRateLimiter;
    private readonly IAiAuditLogger _auditLogger;
    private readonly ILogger<RecipeController> _logger;

    public RecipeController(
        IRecipeService recipeService,
        IValidator<RecipeCreateDto> createDtoValidator,
        IValidator<RecipeUpdateDto> updateDtoValidator,
        IOcrScanService ocrScanService,
        IOptions<FeatureFlagsOptions> flags,
        IAiRateLimiter aiRateLimiter,
        IAiAuditLogger auditLogger,
        ILogger<RecipeController> logger)
    {
        _recipeService = recipeService;
        _createDtoValidator = createDtoValidator;
        _updateDtoValidator = updateDtoValidator;
        _ocrScanService = ocrScanService;
        _flags = flags.Value;
        _aiRateLimiter = aiRateLimiter;
        _auditLogger = auditLogger;
        _logger = logger;
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
        // activated feature verification
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (_flags.ScanRecipeEnabled == false)
        {
            _logger.LogWarning("{EventType} — user {UserId} attempted to call scan while feature is disabled",
                "ScanFeatureDisabledAttempt", userId);
            return StatusCode(503, new { message = "Scan feature disabled in this environment" });
        }

        // Size verification    
        if (imageFile.Length > 10 * 1024 * 1024)
        {
            return BadRequest("File size exceeds 10 MB limit.");
        }

        // Extension verification
        var allowedExtensions = new[] { ".jpeg", ".jpg", ".png" };
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest($"Extension {extension} is not allowed. Allowed: .jpg, .jpeg, .png");
        }

        // MIME type verification
        var allowedMimeTypes = new[] { "image/jpeg", "image/png" };
        var mime = imageFile.ContentType;
        if (!allowedMimeTypes.Contains(mime))
        {
            return BadRequest("MIME type not allowed. Allowed: image/jpeg, image/png");
        }


        //Magic bytes vérification
        using var stream = imageFile.OpenReadStream();
        var magicBytes = new byte[8];
        await stream.ReadExactlyAsync(magicBytes, 0, 8);

        if (!IsValidImageMagicBytes(magicBytes))
        {
            return BadRequest("Invalid image file (magic bytes mismatch).");
        }

        stream.Position = 0; // reset the cursor to the beginning for OCR 

        // Recipe quota check — prevent LLM waste if user already at limit
        await _recipeService.CheckQuotaOrThrowAsync(userId);

        // AI rate limit — LLM-level enforcement (4 tiers)
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        _aiRateLimiter.CheckAndThrow(userId.ToString(), ipAddress);

        // Audit input hash — GDPR Art. 5.1.c minimization (no PII)
        var inputHash = AiInputHasher.Sha256($"{userId}:{imageFile.FileName}:{imageFile.Length}");

        // Timed LLM call for audit trail
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _ocrScanService.ProcessImageAsync(stream);
            stopwatch.Stop();

            await _auditLogger.LogScanSuccessAsync(
                userId,
                result.AiUsage?.ProviderName ?? "unknown",
                result.AiUsage?.PromptTokens ?? 0,
                result.AiUsage?.CompletionTokens ?? 0,
                stopwatch.ElapsedMilliseconds,
                inputHash);

            // Strip audit-only metadata before returning to client (no leak of provider internals)
            result.AiUsage = null;

            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await _auditLogger.LogScanErrorAsync(
                userId,
                provider: "unknown",
                errorCode: ex.GetType().Name,
                stopwatch.ElapsedMilliseconds,
                inputHash);
            throw;
        }
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
        if (magicBytes == null || magicBytes.Length < 8)
        {
            return false;
        }
        byte[] jpegSignature = { 0xFF, 0xD8, 0xFF };
        byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        if (magicBytes.Take(3).SequenceEqual(jpegSignature))
        {
            return true;
        }
        if (magicBytes.Take(8).SequenceEqual(pngSignature))
        {
            return true;
        }
        return false;
    }
}
