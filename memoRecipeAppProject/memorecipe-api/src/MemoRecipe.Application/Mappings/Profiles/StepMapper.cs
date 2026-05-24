using Riok.Mapperly.Abstractions;
using MemoRecipe.Domain.Entities.Steps;
using MemoRecipe.Application.DTOs.Steps;

namespace MemoRecipe.Application.Mappings.Profiles;

[Mapper]
public static partial class StepMapper
{
    [MapperIgnoreSource(nameof(Step.RecipeId))]
    [MapperIgnoreSource(nameof(Step.Recipe))]
    public static partial StepDto ToDto(this Step step);

    [MapperIgnoreTarget(nameof(Step.Id))]
    [MapperIgnoreTarget(nameof(Step.RecipeId))]
    [MapperIgnoreTarget(nameof(Step.Recipe))]
    public static partial Step ToEntity(this StepCreateDto dto);
}