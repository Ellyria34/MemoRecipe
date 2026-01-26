namespace MemoRecipe.Domain.Entities.Nutrition;

public class IngredientNutrition
{
    public Guid Id { get; set; }

    // Name of the ingredient this nutrition profile refers to
    // Example: "Butter", "Whole Milk", "Sugar"
    public string Name { get; set; } = string.Empty;

    // Nutritional values per 100g
    public decimal CaloriesKcal { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal SugarG { get; set; }
    public decimal FatG { get; set; }
    public decimal SaturatedFatG { get; set; }
    public decimal FiberG { get; set; }
    public decimal SaltG { get; set; }

    // Allergens stored as JSON (example: ["gluten", "lactose"])
    public string? AllergensJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation: list of recipe ingredients using this nutrition profile
    public List<Ingredients.Ingredient> Ingredients { get; set; } = new();
}
