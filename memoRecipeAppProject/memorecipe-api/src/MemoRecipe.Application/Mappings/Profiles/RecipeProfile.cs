using AutoMapper;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.DTOs.Ingredients;
using MemoRecipe.Application.DTOs.Steps;
using MemoRecipe.Application.DTOs.Categories;

namespace MemoRecipe.Application.Mappings.Profiles;

public class RecipeProfile : Profile
{
    public RecipeProfile()
    {
        // Create → Entity
        CreateMap<RecipeCreateDto, Recipe>()
            .ForMember(dest => dest.Ingredients, opt => opt.Ignore())
            .ForMember(dest => dest.Steps, opt => opt.Ignore())
            .ForMember(dest => dest.RecipeCategories, opt => opt.Ignore());

        // Entity → DTO
        CreateMap<Recipe, RecipeDto>()
            .ForMember(dest => dest.TotalTimeMinutes,
                       opt => opt.MapFrom(src => src.PrepTimeMinutes + src.CookTimeMinutes))
            .ForMember(dest => dest.Ingredients,
                       opt => opt.MapFrom(src => src.Ingredients))
            .ForMember(dest => dest.Steps,
                       opt => opt.MapFrom(src => src.Steps))
            .ForMember(dest => dest.Categories,
                       opt => opt.MapFrom(src => src.RecipeCategories));
    }
}
