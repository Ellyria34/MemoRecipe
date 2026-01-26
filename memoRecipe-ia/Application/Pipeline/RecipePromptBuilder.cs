using System.Text;

namespace MemoRecipeIA.Application.Pipeline
{
    public static class RecipePromptBuilder
    {
        public static string Build(string ocrText)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a cooking assistant specialized in extracting structured recipes.");
            sb.AppendLine();
            sb.AppendLine("From the OCR text below, extract a recipe and return STRICTLY a valid JSON object.");
            sb.AppendLine("The JSON must match exactly this schema:");
            sb.AppendLine();
            sb.AppendLine("{");
            sb.AppendLine("  \"title\": string,");
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
            sb.AppendLine("Rules:");
            sb.AppendLine("- Do NOT invent ingredients, quantities, or steps.");
            sb.AppendLine("- If a value is missing or unclear, use null.");
            sb.AppendLine("- Correct obvious OCR errors when possible.");
            sb.AppendLine("- Ignore decorative text, icons, headers, footers, and layout artifacts.");
            sb.AppendLine("- Steps must be ordered and written as complete sentences.");
            sb.AppendLine("- Return ONLY raw JSON.");
            sb.AppendLine("- Do NOT include markdown.");
            sb.AppendLine("- Do NOT include explanations or comments.");
            sb.AppendLine();
            sb.AppendLine("OCR TEXT:");
            sb.AppendLine("<<<");
            sb.AppendLine(ocrText);
            sb.AppendLine(">>>");

            return sb.ToString();
        }
    }
}
