namespace MemoRecipe.Web.Models;

public class IngredientFormModel
{
    public string Name { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
}
