using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Domain.Entities.Favorites;

public class Favorite
{
    // Composite key: (UserId, RecipeId)
    
    // User who favorited the recipe
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // Recipe that is marked as favorite
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // Date when the recipe was added to favorites
    public DateTime CreatedAt { get; set; }
}
