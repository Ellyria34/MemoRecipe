using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Services;

namespace MemoRecipe.Web.Components;

public partial class BottomNavBar
{
    [Inject]
    private IFeatureFlagsService FeatureFlags { get; set; } = default!;
    
    [Inject] 
    private ILogger<BottomNavBar> Logger { get; set; } = default!;

    private bool _scanEnabled = false; // fail-safe: default hidden if the API call fails

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var flags = await FeatureFlags.GetAsync();
            _scanEnabled = flags.ScanRecipeEnabled;
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to load feature flags: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);
            // Fallback to _scanEnabled = false (safe default).
        }
    }
}
