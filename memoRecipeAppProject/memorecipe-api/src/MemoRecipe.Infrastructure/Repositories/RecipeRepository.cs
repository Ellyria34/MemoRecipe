using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Application.DTOs.Recipes;
using Microsoft.EntityFrameworkCore;
using MemoRecipe.Application.DTOs.Common;


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

    public async Task<PagedResult<Recipe>> GetAllByUserIdAsync(Guid userId, RecipeQueryParams queryParams)
    {
        IQueryable<Recipe> query = _db.Recipes.Where(r => r.UserId == userId)
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category);

        switch (queryParams.OrderBy?.ToLower())
        {
            case "title":
                query = queryParams.Descending 
                    ? query.OrderByDescending(r => r.Title) 
                    : query.OrderBy(r => r.Title);
                break;
                
            case "createdat":
                query = queryParams.Descending 
                    ? query.OrderByDescending(r => r.CreatedAt) 
                    : query.OrderBy(r => r.CreatedAt);
                break;
                
            default:
                query = query.OrderByDescending(r => r.CreatedAt);
                break;
        }
        
        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        return new PagedResult<Recipe>(items, totalCount, queryParams.Page, queryParams.PageSize);
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

    public async Task<int> CountByUserAsync(Guid userId)
    {
        return await _db.Recipes.CountAsync(r => r.UserId == userId);
    }
}