using MemoRecipeIA.Application.Dtos;
using MemoRecipeIA.Application.Interfaces;

namespace MemoRecipeIA.Application.Pipeline
{
    public class RecipePipeline : IRecipePipeline
    {
        private readonly IOcrService _ocrService;
        private readonly IRecipeAiService _recipeAiService;

        public RecipePipeline(
            IOcrService ocrService,
            IRecipeAiService recipeAiService)
        {
            _ocrService = ocrService;
            _recipeAiService = recipeAiService;
        }

        public async Task<RecipeDto> ProcessAsync(Stream imageStream)
        {
            // Step 1: OCR
            var rawText = await _ocrService.ExtractAsync(imageStream);

            // Step 2: Parsing IA
            var parsed = await _recipeAiService.ParseAsync(rawText);

            // Step 3: Mapping Parsed → RecipeDto
            return new RecipeDto
            {
                Title = parsed.Title,
                Ingredients = parsed.Ingredients
                    .Select(i => $"{i.Quantity} {i.Name}".Trim())
                    .ToList(),
                Steps = parsed.Steps
            };
        }
    }
}
