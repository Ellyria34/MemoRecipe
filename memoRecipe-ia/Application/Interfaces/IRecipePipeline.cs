using MemoRecipeIA.Application.Dtos;

namespace MemoRecipeIA.Application.Interfaces
{
    public interface IRecipePipeline
    {
        Task<RecipeDto> ProcessAsync(Stream imageStream);
    }
}
