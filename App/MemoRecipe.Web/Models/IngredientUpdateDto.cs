namespace MemoRecipe.Web.Models;

public class IngredientUpdateDto
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
}
