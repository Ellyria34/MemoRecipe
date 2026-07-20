using MemoRecipe.Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MemoRecipe.Application.DTOs.Configuration;

namespace MemoRecipe.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly FeatureFlagsOptions _flags;

    public ConfigController(IOptions<FeatureFlagsOptions> flags)
    {
        _flags = flags.Value;
    }

    [HttpGet("features")]
    public IActionResult GetFeatures()
    {
        return Ok(new FeatureFlagsDto
        {
            ScanRecipeEnabled = _flags.ScanRecipeEnabled
        });
    }
}