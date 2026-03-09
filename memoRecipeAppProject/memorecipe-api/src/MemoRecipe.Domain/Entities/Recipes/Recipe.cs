using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Domain.Entities.Ingredients;
using MemoRecipe.Domain.Entities.Steps;
using MemoRecipe.Domain.Entities.Categories;
using MemoRecipe.Domain.Entities.Comments;
using MemoRecipe.Domain.Entities.Favorites;
using MemoRecipe.Domain.Entities.RecipeImages;
using MemoRecipe.Domain.Entities.OCR;
using MemoRecipe.Domain.Entities.Sources;

namespace MemoRecipe.Domain.Entities.Recipes;

public class Recipe
{
    public Guid Id { get; set; }

    // Foreign key to User (required)
    public Guid UserId { get; set; }
    // Navigation property to User (optional when not loaded by EF)
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Servings { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }

    // Computed property, not stored in DB
    public int? TotalTimeMinutes => (PrepTimeMinutes.HasValue || CookTimeMinutes.HasValue)
    ? (PrepTimeMinutes ?? 0) + (CookTimeMinutes ?? 0)
    : null;

    public DifficultyLevel? Difficulty { get; set; }

    // Public or private recipe
    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation collections
    public List<Ingredient> Ingredients { get; set; } = new();
    public List<Step> Steps { get; set; } = new();
    public List<RecipeImage> Images { get; set; } = new();
    public List<RecipeCategory> RecipeCategories { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();

    // Optional 1-to-1 OCR extraction
    public OCRExtraction? OCRExtraction { get; set; }

    // Indicates the source type (manual, OCR, website, book…)
    public RecipeSourceType SourceType { get; set; } = RecipeSourceType.Manual;
    
    public RecipeSource? RecipeSource { get; set; }
}

public enum DifficultyLevel
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}
