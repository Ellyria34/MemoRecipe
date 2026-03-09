using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Application.Repositories;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id);
    Task<List<Recipe>> GetAllByUserIdAsync(Guid userId);
    Task AddAsync(Recipe recipe);
    void Update(Recipe recipe);
    void Delete(Recipe recipe);
    Task SaveChangesAsync();
}