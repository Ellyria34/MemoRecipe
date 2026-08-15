namespace MemoRecipe.Application.DTOs.Recipes
{
    public class AiUsageDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }
}
