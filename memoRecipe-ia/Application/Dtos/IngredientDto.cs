namespace MemoRecipeIA.Application.Dtos
{
    public class IngredientDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
    }
}
