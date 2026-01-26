using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Domain.Entities.Nutrition;

namespace MemoRecipe.Domain.Entities.Ingredients;

public class Ingredient
{
    public Guid Id { get; set; }

    // Foreign key to the recipe this ingredient belongs to
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // Optional foreign key to nutrition data (per 100g)
    public Guid? NutritionId { get; set; }
    public IngredientNutrition? Nutrition { get; set; }

    public string Name { get; set; } = string.Empty;

    // Quantity in the recipe (example: 100g, 2 tbsp, etc.)
    public decimal Quantity { get; set; }

    // Unit used for the quantity (g, ml, tbsp, cup, etc.)
    public string Unit { get; set; } = string.Empty;

    // Optional: group ingredients by section (ex: “Dough”, “Sauce”)
    public string? Section { get; set; }
}
