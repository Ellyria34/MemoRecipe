using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Domain.Entities.Comments;

public class Comment
{
    public Guid Id { get; set; }

    // Foreign key to the user who wrote the comment
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // Foreign key to the recipe this comment belongs to
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // Content of the comment
    public string Content { get; set; } = string.Empty;

    // Optional: rating given by the user (1 to 5 stars)
    public int? Rating { get; set; }

    public DateTime CreatedAt { get; set; }
}
