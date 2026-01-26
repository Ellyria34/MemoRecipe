using AutoMapper;
using MemoRecipe.Domain.Entities.Categories;
using MemoRecipe.Application.DTOs.Categories;

namespace MemoRecipe.Application.Mappings.Profiles;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();

        CreateMap<RecipeCategory, CategoryDto>()
            .ForMember(dest => dest.Id,
                       opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.Name,
                       opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));
    }
}
