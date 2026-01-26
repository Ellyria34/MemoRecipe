using MemoRecipe.Application.DTOs.Ingredients;
using MemoRecipe.Application.DTOs.Steps;

namespace MemoRecipe.Application.DTOs.Recipes;

public class RecipeCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Servings { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public bool IsPublic { get; set; } = true;

    public List<IngredientCreateDto> Ingredients { get; set; } = new();
    public List<StepCreateDto> Steps { get; set; } = new();

    // Ids des catégories sélectionnées
    public List<Guid> CategoryIds { get; set; } = new();
}
