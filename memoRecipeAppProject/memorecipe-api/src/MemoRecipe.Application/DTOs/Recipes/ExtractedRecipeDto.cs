namespace MemoRecipe.Application.DTOs.Recipes
{
    public class ExtractedRecipeDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Servings { get; set; }
        public int? PrepTimeMinutes { get; set; }
        public int? CookTimeMinutes { get; set; }
        public string? PreparationTime { get; set; } 
        public string? Difficulty { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public List<string> Steps { get; set; } = new();
    }
}