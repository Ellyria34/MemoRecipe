using AutoMapper;
using MemoRecipe.Domain.Entities.Steps;
using MemoRecipe.Application.DTOs.Steps;

namespace MemoRecipe.Application.Mappings.Profiles;

public class StepProfile : Profile
{
    public StepProfile()
    {
        CreateMap<StepCreateDto, Step>();
        CreateMap<Step, StepDto>();
    }
}
