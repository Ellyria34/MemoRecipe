using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Domain.Entities.Sources;

public class RecipeSource
{
    public Guid Id { get; set; }

    // Foreign key to the recipe
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // Optional: URL if the recipe comes from a website
    public string? Url { get; set; }

    // Optional: book name if the recipe comes from a printed source
    public string? BookTitle { get; set; }

    // Optional: page number in the book
    public int? BookPage { get; set; }

    // Optional: additional meta data (JSON)
    // Example:
    // { "magazine": "Cuisine Actuelle", "issue": "October 2023" }
    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; }
}