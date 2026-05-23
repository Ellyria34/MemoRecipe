using Riok.Mapperly.Abstractions;
using MemoRecipe.Domain.Entities.Ingredients;
using MemoRecipe.Application.DTOs.Ingredients;

namespace MemoRecipe.Application.Mappings.Profiles;

[Mapper]
public static partial class IngredientMapper
{
    [MapperIgnoreSource(nameof(Ingredient.RecipeId))]
    [MapperIgnoreSource(nameof(Ingredient.Recipe))]
    [MapperIgnoreSource(nameof(Ingredient.NutritionId))]
    [MapperIgnoreSource(nameof(Ingredient.Nutrition))]
    [MapperIgnoreSource(nameof(Ingredient.Section))]
    public static partial IngredientDto ToDto(this Ingredient ingredient);

    [MapperIgnoreTarget(nameof(Ingredient.Id))]
    [MapperIgnoreTarget(nameof(Ingredient.RecipeId))]
    [MapperIgnoreTarget(nameof(Ingredient.Recipe))]
    [MapperIgnoreTarget(nameof(Ingredient.NutritionId))]
    [MapperIgnoreTarget(nameof(Ingredient.Nutrition))]
    [MapperIgnoreTarget(nameof(Ingredient.Section))]
    public static partial Ingredient ToEntity(this IngredientCreateDto dto);
}