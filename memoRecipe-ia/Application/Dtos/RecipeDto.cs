namespace MemoRecipeIA.Application.Dtos
{
    public class RecipeDto
    {
        public string Title { get; set; } = string.Empty;
        public int Servings { get; set; }
        public string PreparationTime { get; set; } = "";
        public List<string> Ingredients { get; set; } = new();
        public List<string> Steps { get; set; } = new();
    }
}
