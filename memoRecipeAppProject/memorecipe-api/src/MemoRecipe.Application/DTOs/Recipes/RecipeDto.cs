using MemoRecipe.Application.DTOs.Ingredients;
using MemoRecipe.Application.DTOs.Steps;
using MemoRecipe.Application.DTOs.Categories;

namespace MemoRecipe.Application.DTOs.Recipes;

public class RecipeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Servings { get; set; }
    public int TotalTimeMinutes { get; set; }

    public List<IngredientDto> Ingredients { get; set; } = new();
    public List<StepDto> Steps { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}
