using Riok.Mapperly.Abstractions;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Application.DTOs.Categories;
using MemoRecipe.Application.DTOs.Ingredients;
using MemoRecipe.Application.DTOs.Steps;

namespace MemoRecipe.Application.Mappings.Profiles;

[Mapper]
[UseStaticMapper(typeof(IngredientMapper))]
[UseStaticMapper(typeof(StepMapper))]
[UseStaticMapper(typeof(CategoryMapper))]
public static partial class RecipeMapper
{
    [MapperIgnoreTarget(nameof(Recipe.Id))]
    [MapperIgnoreTarget(nameof(Recipe.UserId))]
    [MapperIgnoreTarget(nameof(Recipe.User))]
    [MapperIgnoreTarget(nameof(Recipe.CreatedAt))]
    [MapperIgnoreTarget(nameof(Recipe.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Recipe.Ingredients))]
    [MapperIgnoreTarget(nameof(Recipe.Steps))]
    [MapperIgnoreTarget(nameof(Recipe.RecipeCategories))]
    [MapperIgnoreTarget(nameof(Recipe.Images))]
    [MapperIgnoreTarget(nameof(Recipe.Comments))]
    [MapperIgnoreTarget(nameof(Recipe.Favorites))]
    [MapperIgnoreTarget(nameof(Recipe.OCRExtraction))]
    [MapperIgnoreTarget(nameof(Recipe.RecipeSource))]
    [MapperIgnoreSource(nameof(RecipeCreateDto.CategoryIds))]
    [MapperIgnoreSource(nameof(RecipeCreateDto.Ingredients))]
    [MapperIgnoreSource(nameof(RecipeCreateDto.Steps))]
    [MapperIgnoreTarget(nameof(Recipe.SourceType))]

    public static partial Recipe ToEntity(this RecipeCreateDto dto);

    [MapperIgnoreSource(nameof(Recipe.User))]
    [MapperIgnoreSource(nameof(Recipe.Images))]
    [MapperIgnoreSource(nameof(Recipe.Comments))]
    [MapperIgnoreSource(nameof(Recipe.Favorites))]
    [MapperIgnoreSource(nameof(Recipe.OCRExtraction))]
    [MapperIgnoreSource(nameof(Recipe.SourceType))]
    [MapperIgnoreSource(nameof(Recipe.RecipeSource))]
    [MapProperty(nameof(Recipe.RecipeCategories), nameof(RecipeDto.Categories))]
    public static partial RecipeDto ToDto(this Recipe recipe);
}