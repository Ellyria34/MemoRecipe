namespace MemoRecipeIA.Application.Dtos
{
    public class ParsedRecipeDto
    {
        public string Title { get; set; } = string.Empty;
        public int? Servings { get; set; }
        public List<ParsedIngredientDto> Ingredients { get; set; } = new();
        public List<string> Steps { get; set; } = new();
    }
}
