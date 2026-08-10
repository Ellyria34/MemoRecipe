namespace MemoRecipeIA.Application.Dtos;

public class ParsedRecipeDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Servings { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public string? Difficulty { get; set; }
    public List<ParsedIngredientDto> Ingredients { get; set; } = new();
    public List<string> Steps { get; set; } = new();
}
