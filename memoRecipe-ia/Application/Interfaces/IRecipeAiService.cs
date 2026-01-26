using MemoRecipeIA.Application.Dtos;

namespace MemoRecipeIA.Application.Interfaces
{
    public interface IRecipeAiService
    {
        Task<ParsedRecipeDto> ParseAsync(string ocrText);
    }
}
