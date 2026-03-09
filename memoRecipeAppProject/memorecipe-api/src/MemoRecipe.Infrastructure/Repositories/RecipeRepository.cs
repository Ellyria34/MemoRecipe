using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;


namespace MemoRecipe.Infrastructure.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private readonly MemoRecipeDbContext _db;
    public RecipeRepository(MemoRecipeDbContext db)
    {
        _db = db;
    }

    public async Task<Recipe?> GetByIdAsync(Guid id)
    {
        var recipe = await _db.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        return recipe;
    }

    public async Task<List<Recipe>> GetAllByUserIdAsync(Guid userId)
    {
        var recipes = await _db.Recipes.Where(r => r.UserId == userId)
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
            .ToListAsync();
        
        return recipes;
    }

    public async Task AddAsync(Recipe recipe)
    {
        await _db.Recipes.AddAsync(recipe);
    }

    public void Update(Recipe recipe)
    {
        _db.Recipes.Update(recipe);
    }
    public void Delete(Recipe recipe)
    {
        _db.Recipes.Remove(recipe);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}