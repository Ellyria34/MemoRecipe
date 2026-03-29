namespace MemoRecipe.Web.Models;

public class RecipeUpdateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Servings { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public DifficultyLevel? Difficulty { get; set; }
    public bool? IsPublic { get; set; }
    public List<IngredientUpdateDto>? Ingredients { get; set; }
    public List<StepUpdateDto>? Steps { get; set; }
}
