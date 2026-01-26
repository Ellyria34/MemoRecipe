using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Domain.Entities.RecipeImages;

public class RecipeImage
{
    public Guid Id { get; set; }

    // Foreign key to the recipe
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // URL to the image (Cloud storage, CDN, etc.)
    public string Url { get; set; } = string.Empty;

    // Optional: alternative text for accessibility and SEO
    public string? AltText { get; set; }

    // Order of the image in the gallery
    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }
}
