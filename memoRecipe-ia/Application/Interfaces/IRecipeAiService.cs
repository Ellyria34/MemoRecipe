using MemoRecipeIA.Application.Dtos;

namespace MemoRecipeIA.Application.Interfaces
{
    public interface IRecipeAiService
    {
        Task<(ParsedRecipeDto Parsed, AiUsageDto Usage)> ParseAsync(string ocrText);
    }
}
