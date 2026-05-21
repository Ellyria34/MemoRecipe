using Microsoft.AspNetCore.Components;
using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Components;

public partial class RecipeListCard
{
    [Parameter]
    public RecipeDto Recipe { get; set; } = default!;

    private static string GetDifficultyLabel(DifficultyLevel? difficulty) => difficulty switch
    {
        DifficultyLevel.Easy => "Facile",
        DifficultyLevel.Medium => "Moyen",
        DifficultyLevel.Hard => "Difficile",
        _ => ""
    };
}
