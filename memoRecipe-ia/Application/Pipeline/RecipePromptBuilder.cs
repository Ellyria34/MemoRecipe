namespace MemoRecipeIA.Application.Pipeline;

public static class RecipePromptBuilder
{
    private const string SchemaAndRules = """
        You are an information extraction system.
        Extract a recipe strictly from what is visible in the provided source.

        Return STRICTLY a valid JSON object matching EXACTLY this schema:

        {
          "title": string | null,
          "description": string | null,
          "servings": number | null,
          "prepTimeMinutes": number | null,
          "cookTimeMinutes": number | null,
          "difficulty": "easy" | "medium" | "hard" | null,
          "ingredients": [
            { "name": string, "quantity": string | null }
          ],
          "steps": [ string ]
        }

        Rules:
        - Extract information ONLY from what is visible in the source.
        - Do NOT invent ingredients, quantities, or steps.
        - Preserve the ORIGINAL step count and order.
        - Include the FULL title including subtitles.
        - Do NOT normalize or transform quantities (no unit conversion, no reformatting).
        - If a value is ambiguous or unclear, return null rather than guessing.
        - Return raw JSON only. No markdown, no explanations, no code fences.
        """;

    public static string BuildForText(string ocrText) =>
        $"""
        {SchemaAndRules}

        OCR TEXT:
        <<<
        {ocrText}
        >>>
        """;

    public static string BuildForVision() => SchemaAndRules;
}