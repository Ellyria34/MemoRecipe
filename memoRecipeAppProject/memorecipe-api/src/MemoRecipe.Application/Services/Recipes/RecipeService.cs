using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Categories;
using MemoRecipe.Domain.Entities.Ingredients;
using MemoRecipe.Domain.Entities.Steps;
using MemoRecipe.Application.Mappings.Profiles;
using MemoRecipe.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace MemoRecipe.Application.Services.Recipes;

public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RecipeService> _logger;


    public RecipeService(IRecipeRepository repository, IUserRepository userRepository, ILogger<RecipeService> logger)
    {
        _repository = repository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<RecipeDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var recipe = await _repository.GetByIdAsync(id);
        if (recipe == null)
        {
            return null;
        }

        if (recipe.UserId != userId && !recipe.IsPublic)
        {
            _logger.LogWarning("{EventType} — user {UserId} attempted to access private recipe {RecipeId} owned by another user",
                "UnauthorizedRecipeRead", userId, id);
            return null;
        }
        
        return recipe.ToDto();
    }

    public async Task<List<RecipeDto>> GetAllByUserAsync(Guid userId, RecipeQueryParams queryParams)
    {
        var recipes = await _repository.GetAllByUserIdAsync(userId, queryParams);
        return recipes.Select(r => r.ToDto()).ToList();
    }

    public async Task<RecipeDto> CreateAsync(RecipeCreateDto dto, Guid userId)
    {
        await EnsureAccountActiveAsync(userId);

        var newRecipe = dto.ToEntity();
        newRecipe.Id = Guid.NewGuid();
        newRecipe.UserId = userId;
        newRecipe.CreatedAt = DateTime.UtcNow;
        newRecipe.UpdatedAt = DateTime.UtcNow;

        newRecipe.RecipeCategories = dto.CategoryIds.Select(categoryId => new RecipeCategory
        {
            RecipeId = newRecipe.Id,
            CategoryId = categoryId
        }).ToList();

        newRecipe.Ingredients = dto.Ingredients.Select(ingredient => new Ingredient
        {
            Id = Guid.NewGuid(),
            RecipeId = newRecipe.Id,
            Name = ingredient.Name,
            Quantity = ingredient.Quantity,
            Unit = ingredient.Unit
        }).ToList();


        newRecipe.Steps = dto.Steps.Select((s, index) => new Step
        {
            Id = Guid.NewGuid(),
            RecipeId = newRecipe.Id,
            Instruction = s.Instruction,
            Order = s.Order > 0 ? s.Order : index + 1
        }).ToList();

        await _repository.AddAsync(newRecipe);
        await _repository.SaveChangesAsync();

        var recipeCreated = await _repository.GetByIdAsync(newRecipe.Id);
        if (recipeCreated == null)
        {
            throw new InvalidOperationException("Recipe was not found after creation");
        }
        return recipeCreated.ToDto();
    }

    public async Task<RecipeDto?> UpdateAsync(Guid id, RecipeUpdateDto dto, Guid userId)
    {
        await EnsureAccountActiveAsync(userId);

        var recipe = await _repository.GetByIdAsync(id);
        if (recipe == null)
        {
            return null;
        }
        if (recipe.UserId != userId)
        {
            _logger.LogWarning("{EventType} — user {UserId} attempted to update recipe {RecipeId} owned by another user",
                "UnauthorizedRecipeUpdate", userId, id);
            return null;
        }

        if (dto.Title != null) recipe.Title = dto.Title;
        if (dto.Description != null) recipe.Description = dto.Description;
        if (dto.Servings != null) recipe.Servings = dto.Servings;
        if (dto.PrepTimeMinutes != null) recipe.PrepTimeMinutes = dto.PrepTimeMinutes;
        if (dto.CookTimeMinutes != null) recipe.CookTimeMinutes = dto.CookTimeMinutes;
        if (dto.Difficulty != null) recipe.Difficulty = dto.Difficulty;
        if (dto.IsPublic != null) recipe.IsPublic = dto.IsPublic.Value;
        if (dto.Ingredients != null)
        {
            recipe.Ingredients.Clear();
            recipe.Ingredients = dto.Ingredients.Select(i => new Ingredient
            {
                RecipeId = recipe.Id,
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit
            }).ToList();
        }
        if (dto.Steps != null)
        {
            recipe.Steps.Clear();
            recipe.Steps = dto.Steps.Select((s, index) => new Step
            {
                RecipeId = recipe.Id,
                Instruction = s.Instruction,
                Order = index + 1
            }).ToList();
        }

        recipe.UpdatedAt = DateTime.UtcNow;
        if (dto.CategoryIds != null)
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

        return recipe.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        await EnsureAccountActiveAsync(userId);

        var recipe = await _repository.GetByIdAsync(id);
        if (recipe == null)
        {
            return false;
        }

        if (recipe.UserId != userId)
        {
            _logger.LogWarning("{EventType} — user {UserId} attempted to delete recipe {RecipeId} owned by another user",
                "UnauthorizedRecipeDelete", userId, id);
            return false;
        }
        _repository.Delete(recipe);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountByUserAsync(Guid userId)
    {
        var result = await _repository.CountByUserAsync(userId);
        if (result == null)
        {
            return 0;
        }
        return result;
    }

    private async Task EnsureAccountActiveAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user?.DeleteRequestedAt != null)
        {
            throw new AccountMarkedForDeletionException();
        }
    }

}