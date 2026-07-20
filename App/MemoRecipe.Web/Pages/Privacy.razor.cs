using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Services;

namespace MemoRecipe.Web.Pages;

public partial class Privacy
{
    [Inject]
    private IFeatureFlagsService FeatureFlags { get; set; } = default!;

    [Inject]
    private ILogger<Privacy> Logger { get; set; } = default!;

    private bool _scanEnabled = false; // fail-safe: default to "AI disabled" notice if the API call fails

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
            // Fallback to _scanEnabled = false: safer to show "no AI" than to falsely claim AI is running.
        }
    }
}
