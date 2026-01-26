namespace MemoRecipe.Domain.Entities.Categories;

public class Category
{
    public Guid Id { get; set; }

    // Category name (e.g., "Dessert", "Vegan", "Christmas")
    public string Name { get; set; } = string.Empty;

    // Optional: user-friendly URL name (e.g. "quick-meals")
    public string? Slug { get; set; }

    // Optional: description shown in the blog or UI
    public string? Description { get; set; }

    // Navigation to the pivot table (many-to-many)
    public List<RecipeCategory> RecipeCategories { get; set; } = new();
}
