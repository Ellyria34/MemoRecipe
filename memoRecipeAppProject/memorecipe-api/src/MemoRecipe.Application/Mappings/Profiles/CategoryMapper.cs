using Riok.Mapperly.Abstractions;
using MemoRecipe.Domain.Entities.Categories;
using MemoRecipe.Application.DTOs.Categories;

namespace MemoRecipe.Application.Mappings.Profiles;

[Mapper]
public static partial class CategoryMapper
{
    [MapperIgnoreSource(nameof(Category.Slug))]
    [MapperIgnoreSource(nameof(Category.Description))]
    [MapperIgnoreSource(nameof(Category.RecipeCategories))]
    public static partial CategoryDto ToDto(this Category category);

    [MapProperty(nameof(RecipeCategory.CategoryId), nameof(CategoryDto.Id))]
    [MapProperty(nameof(RecipeCategory.Category) + "." + nameof(Category.Name), nameof(CategoryDto.Name))]
    [MapperIgnoreSource(nameof(RecipeCategory.RecipeId))]
    [MapperIgnoreSource(nameof(RecipeCategory.Recipe))]

    public static partial CategoryDto ToCategoryDto(this RecipeCategory entity);

}