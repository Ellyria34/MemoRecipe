namespace MemoRecipe.Web.Models;

public class RecipeFormModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Servings { get; set; } = 1;
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }

    public DifficultyLevel? Difficulty { get; set; }
    public bool IsPublic { get; set; } = true;

    // Navigation collections
    public List<IngredientFormModel> Ingredients { get; set; } = new();
    public List<StepFormModel> Steps { get; set; } = new();
}