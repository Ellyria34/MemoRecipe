namespace MemoRecipeIA.Application.Dtos
{
    public class ParsedIngredientDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Quantity { get; set; }
    }
}