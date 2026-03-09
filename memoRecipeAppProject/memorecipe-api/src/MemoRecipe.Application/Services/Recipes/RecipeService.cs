using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Domain.Entities.Categories;
using AutoMapper;

namespace MemoRecipe.Application.Services.Recipes;
 
public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _repository;
    private readonly IMapper _mapper;

    public RecipeService(IRecipeRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<RecipeDto?> GetByIdAsync(Guid id)
    {
        var recipe = await _repository.GetByIdAsync(id);
        return _mapper.Map<RecipeDto>(recipe);
    }

    public async Task<List<RecipeDto>> GetAllByUserAsync(Guid userId)
    {
        var recipes = await _repository.GetAllByUserAsync(userId);
        return _mapper.Map<List<RecipeDto>>(recipes);
    }

    public async Task<RecipeDto> CreateAsync(RecipeCreateDto dto, Guid userId)
    {
        var newRecipe = _mapper.Map<Recipe>(dto);
        newRecipe.Id = Guid.NewGuid();
        newRecipe.UserId = userId;
        newRecipe.CreatedAt = DateTime.UtcNow;
        newRecipe.UpdatedAt = DateTime.UtcNow;

        newRecipe.RecipeCategories = dto.CategoryIds.Select(categoryId => new RecipeCategory
        {
            RecipeId = newRecipe.Id,
            CategoryId = categoryId
        }).ToList();

        await _repository.AddAsync(newRecipe);
        await _repository.SaveChangesAsync();

        var recipeCreated = await _repository.GetByIdAsync(newRecipe.Id);
        if(recipeCreated == null)
        {
            throw new InvalidOperationException("Recipe was not found after creation");
        }
        return _mapper.Map<RecipeDto>(recipeCreated);
    }

    public async Task<RecipeDto?> UpdateAsync(Guid id, RecipeUpdateDto dto)
    {
        var recipe = await _repository.GetByIdAsync(id);
        if (recipe == null) 
        {
            return null;
        }

        if(dto.Title != null) recipe.Title = dto.Title;
        if(dto.Description != null) recipe.Description = dto.Description;
        if(dto.Servings != null) recipe.Servings = dto.Servings;
        if(dto.PrepTimeMinutes != null) recipe.PrepTimeMinutes = dto.PrepTimeMinutes;
        if(dto.CookTimeMinutes != null) recipe.CookTimeMinutes = dto.CookTimeMinutes;
        if(dto.Difficulty != null) recipe.Difficulty = dto.Difficulty;
        if(dto.IsPublic != null) recipe.IsPublic = dto.IsPublic.Value;
        // if(dto.Ingredients != null) recipe.Ingredients = dto.Ingredients;
        // if(dto.Steps != null) recipe.Steps = dto.Steps;
        recipe.UpdatedAt = DateTime.UtcNow;
        if(dto.CategoryIds != null)
        {
            recipe.RecipeCategories.Clear();
            recipe.RecipeCategories = dto.CategoryIds.Select(categoryId => new RecipeCategory
            {
                RecipeId = recipe.Id,
                CategoryId = categoryId
            }).ToList();
        }

        _repository.Update(recipe);
        await _repository.SaveChangesAsync();
        
        return _mapper.Map<RecipeDto>(recipe);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var recipe = await _repository.GetByIdAsync(id);
        if(recipe == null) 
        {
            return false;
        }
        _repository.Delete(recipe);
        await _repository.SaveChangesAsync();
        return true;
    }
}