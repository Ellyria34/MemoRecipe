using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Domain.Entities.Categories;

public class RecipeCategory
{
    // Composite key: (RecipeId, CategoryId)
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
}
