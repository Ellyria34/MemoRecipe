using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Domain.Entities.OCR;

public class OCRExtraction
{
    public Guid Id { get; set; }

    // Foreign key to the recipe
    public Guid RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    // Raw text extracted from OCR (unprocessed)
    public string RawText { get; set; } = string.Empty;

    // Processed and structured OCR data (JSON format)
    // Example: { "ingredients": [...], "steps": [...] }
    public string? JsonData { get; set; }

    // Original image URL (optional)
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
