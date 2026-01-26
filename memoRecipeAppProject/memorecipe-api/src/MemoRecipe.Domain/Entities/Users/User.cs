namespace MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Domain.Entities.Comments;
using MemoRecipe.Domain.Entities.Favorites;
using MemoRecipe.Domain.Entities.Recipes;


public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public List<Recipe> Recipes { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();
}

public enum UserRole
{
    User = 0,
    Admin = 1
}
