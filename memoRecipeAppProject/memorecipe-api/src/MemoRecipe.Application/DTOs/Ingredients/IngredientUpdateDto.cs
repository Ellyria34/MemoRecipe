namespace MemoRecipe.Application.DTOs.Ingredients;

public class IngredientUpdateDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
}
