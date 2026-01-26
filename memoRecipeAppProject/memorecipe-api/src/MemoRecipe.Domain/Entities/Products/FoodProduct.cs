using MemoRecipe.Domain.Entities.Nutrition;

namespace MemoRecipe.Domain.Entities.Products;

public class FoodProduct
{
    public Guid Id { get; set; }

    // Barcode in EAN-13 format (example: "3017620422003")
    public string Barcode { get; set; } = string.Empty;

    // Product name and brand
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }

    // Optional data from OpenFoodFacts
    public string? NutriScore { get; set; }      // a, b, c, d, e
    public int? NovaGroup { get; set; }          // 1 to 4

    // Link to nutrition data (per 100g)
    public Guid NutritionId { get; set; }
    public IngredientNutrition? Nutrition { get; set; }

    public DateTime CreatedAt { get; set; }
}
