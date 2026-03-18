namespace MemoRecipe.Web.Models;

public class RecipeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Servings { get; set; }
    public int? TotalTimeMinutes { get; set; }
    public int? PrepTimeMinutes {get; set;}
    public int? CookTimeMinutes {get; set;}
    public bool IsPublic {get; set;}
    public Guid UserId {get; set;}
    public DifficultyLevel?  Difficulty {get; set;}
    public List<IngredientDto> Ingredients { get; set; } = new();
    public List<StepDto> Steps { get; set; } = new();
    public List<CategoryDto>? Categories { get; set; } = new();
}
