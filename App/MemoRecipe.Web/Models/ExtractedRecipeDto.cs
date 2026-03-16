namespace MemoRecipe.Web.Models;

public class ExtractedRecipeDto
{
    public string Title { get; set; } ="";
    public int? Servings { get; set; }
    public string? PreparationTime { get; set; }
    public List<string> Ingredients {get; set; } = new();
    public List<string> Steps {get; set; } = new();
}