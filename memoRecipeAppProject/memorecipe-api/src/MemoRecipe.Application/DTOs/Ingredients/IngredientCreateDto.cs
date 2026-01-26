namespace MemoRecipe.Application.DTOs.Ingredients;

public class IngredientCreateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
}
