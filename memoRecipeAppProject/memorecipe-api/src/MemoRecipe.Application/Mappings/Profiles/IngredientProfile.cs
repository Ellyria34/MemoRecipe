using AutoMapper;
using MemoRecipe.Domain.Entities.Ingredients;
using MemoRecipe.Application.DTOs.Ingredients;

namespace MemoRecipe.Application.Mappings.Profiles;

public class IngredientProfile : Profile
{
    public IngredientProfile()
    {
        CreateMap<IngredientCreateDto, Ingredient>();
        CreateMap<Ingredient, IngredientDto>();
    }
}
