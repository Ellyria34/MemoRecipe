using System.Text;

namespace MemoRecipeIA.Application.Pipeline
{
    public static class RecipePromptBuilder
    {
        public static string Build(string ocrText)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are an information extraction system.");
            sb.AppendLine("Your task is to STRUCTURE information, not to complete it.");
            sb.AppendLine();

            sb.AppendLine("You must extract a recipe from the OCR text below.");
            sb.AppendLine("Return STRICTLY a valid JSON object matching EXACTLY this schema:");
            sb.AppendLine();

            sb.AppendLine("{");
            sb.AppendLine("  \"title\": string | null,");
            sb.AppendLine("  \"servings\": number | null,");
            sb.AppendLine("  \"ingredients\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"name\": string,");
            sb.AppendLine("      \"quantity\": string | null");
            sb.AppendLine("    }");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"steps\": [ string ]");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("IMPORTANT RULES (MANDATORY):");
            sb.AppendLine();
            sb.AppendLine("- NEVER guess missing digits in quantities.");
            sb.AppendLine("- If a quantity is ambiguous (e.g. \"[15g\"), return it exactly as seen OR set the quantity to null.");
            sb.AppendLine("- You MUST NOT invent ingredients, quantities, or steps.");
            sb.AppendLine("- EVERY ingredient name and quantity MUST appear explicitly in the OCR text.");
            sb.AppendLine("- Minor OCR typo correction is allowed ONLY if the intended word is obvious.");
            sb.AppendLine("- If you are NOT 100% sure a value exists in the OCR text, set it to null or omit it.");
            sb.AppendLine("- Do NOT infer missing quantities.");
            sb.AppendLine("- Do NOT normalize or transform quantities (no unit conversion, no reformatting).");
            sb.AppendLine("- Do NOT replace ingredient names with more common equivalents.");
            sb.AppendLine("- If the OCR text is ambiguous, prefer null.");
            sb.AppendLine();
            sb.AppendLine("OUTPUT CONSTRAINTS:");
            sb.AppendLine();
            sb.AppendLine("- Return ONLY raw JSON.");
            sb.AppendLine("- Do NOT include markdown.");
            sb.AppendLine("- Do NOT include explanations, comments, or extra text.");
            sb.AppendLine("- If these rules are violated, the output is considered INVALID.");
            sb.AppendLine();

            sb.AppendLine("OCR TEXT:");
            sb.AppendLine("<<<");
            sb.AppendLine(ocrText);
            sb.AppendLine(">>>");

            return sb.ToString();
        }
    }
}
