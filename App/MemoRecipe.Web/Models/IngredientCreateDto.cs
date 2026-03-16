namespace MemoRecipe.Web.Models;

public class IngredientCreateDto
{
    public string Name { get; set; } = string.Empty;

    // Quantity in the recipe (example: 100g, 2 tbsp, etc.)
    public decimal Quantity { get; set; }

    // Unit used for the quantity (g, ml, tbsp, cup, etc.)
    public string? Unit { get; set; }

}