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
using MemoRecipe.Application.Services.Upload;

namespace MemoRecipe.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IValidator<RecipeCreateDto> _createDtoValidator;
    private readonly IValidator<RecipeUpdateDto> _updateDtoValidator;
    private readonly IFileUploadValidator _fileUploadValidator;
    private readonly IOcrScanService _ocrScanService;
    private readonly FeatureFlagsOptions _flags;
    private readonly IAiRateLimiter _aiRateLimiter;
    private readonly IAiAuditLogger _auditLogger;
    private readonly ILogger<RecipeController> _logger;

    public RecipeController(
        IRecipeService recipeService,
        IValidator<RecipeCreateDto> createDtoValidator,
        IValidator<RecipeUpdateDto> updateDtoValidator,
        IFileUploadValidator fileUploadValidator,
        IOcrScanService ocrScanService,
        IOptions<FeatureFlagsOptions> flags,
        IAiRateLimiter aiRateLimiter,
        IAiAuditLogger auditLogger,
        ILogger<RecipeController> logger)
    {
        _recipeService = recipeService;
        _createDtoValidator = createDtoValidator;
        _updateDtoValidator = updateDtoValidator;
        _fileUploadValidator = fileUploadValidator;
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

        // Upload validation (extension + MIME + magic bytes)
        using var stream = imageFile.OpenReadStream();
        var validation = await _fileUploadValidator.ValidateAsync(
            stream,
            imageFile.FileName,
            imageFile.ContentType);

        switch (validation)
        {
            case FileUploadValidationResult.Valid:
                break;
            case FileUploadValidationResult.ExtensionNotAllowed:
                return BadRequest("Extension not allowed. Allowed: .jpg, .jpeg, .png");
            case FileUploadValidationResult.MimeTypeNotAllowed:
                return BadRequest("MIME type not allowed. Allowed: image/jpeg, image/png");
            case FileUploadValidationResult.FileTooSmall:
            case FileUploadValidationResult.InvalidMagicBytes:
                // Same generic message for both — don't reveal the 8-byte threshold to attackers (US-03 security)
                return BadRequest("Invalid or corrupted image file.");
        }


        // Recipe quota check — prevent LLM waste if user already at limit
        await _recipeService.EnsureQuotaAvailableAsync(userId);

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
}
